// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Search;
using NuGet.Services.V3Repository;
using Xunit;

using PackageFilter = System.Func<System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>,
                                  System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>>;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public class DatabaseSearchUnitTests
    {
        private readonly DatabaseSearchService _databaseSearchService;
        private readonly StageContextMock _stageContextMock;

        public static IEnumerable<object[]> _queryVerificationTestInput = new List<object[]>
        {
             new object[]
            {
                "Test simple query", true, "title1", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Title.Contains("title1")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test take + empty query", true, "", 0, 10, new PackageFilter(allPackages => allPackages.OrderBy(x => x.Id).Take(10))
            },
            new object[]
            {
                "Test skip", true, "", 15, 200, new PackageFilter(allPackages => allPackages.OrderBy(x => x.Id).Skip(15))
            },
            new object[]
            {
                "Test prerelease", false, "", 0, 200, new PackageFilter(allPackages => allPackages.Where(p => !p.IsPrerelease).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test packageid field", true, QueryField.PackageId +":json5", 0, 10, new PackageFilter(allPackages => allPackages.Where(p => p.Id == "json5").OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test id field", true, QueryField.Id +":json5", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Id.Contains("json5")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test version field", true, QueryField.Version +":2", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Version == "2.0.0").OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test illegal version field", true, QueryField.Version +":abc", 0, 100, new PackageFilter(allPackages => new List<PackageMetadata>())
            },
            new object[]
            {
                "Test title field", true, QueryField.Title +":title2", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Title.Contains("title2")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test description field", true, QueryField.Description +":NuGet4", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Description.Contains("NuGet4")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test tag field", true, QueryField.Tag +":test5", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Tags.Contains("test5")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test author field", true, QueryField.Author +":nuget27", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test summary field", true, QueryField.Summary +":nuget6", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Summary.Contains("nuget6")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test owner field", true, QueryField.Owner +":owners9", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Owners.Contains("owners9")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test empty results", true, "abc", 0, 100, new PackageFilter(allPackages => new List<PackageMetadata>())
            },
            new object[]
            {
                "Test search with multiple fields",
                true,
                $"{QueryField.Owner}:owners9 {QueryField.Author}:nuget27",
                0,
                100,
                new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27") || p.Owners.Contains("owners9")).OrderBy(x => x.Id))
            },
            new object[]
            {
                "Test search with field and text",
                true,
                $"owners9 {QueryField.Author}:nuget27",
                0,
                100,
                new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27") || p.Owners.Contains("owners9")).OrderBy(x => x.Id))
            }
        }; 

        public DatabaseSearchUnitTests()
        {
            _stageContextMock = new StageContextMock();
            var pathGenerator = new V3PathGenerator(new Uri($"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/"));
            _databaseSearchService = new DatabaseSearchService(_stageContextMock.Object, pathGenerator, DataMockHelper.DefaultStageId);    
        }

        [Theory]
        [MemberData("_queryVerificationTestInput")]
        public void VerifySearchInternal(string testName, bool includePrerelease, string query, int skip, int take, PackageFilter expectedResultGenerator)
        {
            // Arrange
            var allPackages = _stageContextMock.AddMockPackageMetadataList();

            // Act
            var filteredPackages = _databaseSearchService.SearchInternal(DataMockHelper.DefaultStageKey, includePrerelease, query, skip, take).ToList();

            // Assert
            var expectedPackages = expectedResultGenerator(allPackages).ToList();

            filteredPackages.Count.Should().Be(expectedPackages.Count, $"for test {testName} count should be the same");
            filteredPackages.Should().Equal(expectedPackages, (x, y) => x.Id == y.Id && x.Version == y.Version, $"for test {testName} lists should be identical");
        }

        [Fact]
        public void VerifySearchInternalForPackageWithMultipleVersions()
        {
            // Arrange
            const string firstId = "aaa";
            const string secondId = "bbb";

            var package1 = DataMockHelper.CreateDefaultPackageMetadata(firstId);
            var package2 = DataMockHelper.CreateDefaultPackageMetadata(firstId, version: "2.0.0");
            var package3 = DataMockHelper.CreateDefaultPackageMetadata(secondId);
            var package4 = DataMockHelper.CreateDefaultPackageMetadata(secondId, version: "2.0.0");

            _stageContextMock.Object.PackagesMetadata.AddRange(new List<PackageMetadata> { package1, package2, package3, package4 });

            // Act
            var filteredPackages = _databaseSearchService.SearchInternal(DataMockHelper.DefaultStageKey, includePrerelease: true, query: "", skip: 1, take: 4).ToList();

            // Assert

            // The "firstId" packages should have been skipped
            filteredPackages.Should().HaveCount(2);
            filteredPackages.Should().NotContain(x => x.Id == firstId);
        }

        [Fact]
        public void VerifySearchInternalFilteringByStage()
        {
            // Arrange
            _stageContextMock.AddMockPackageMetadataList(stageKey: DataMockHelper.DefaultStageKey);
            _stageContextMock.AddMockPackageMetadataList(stageKey: DataMockHelper.DefaultStageKey + 1);

            // Act
            var filteredPackages = _databaseSearchService.SearchInternal(DataMockHelper.DefaultStageKey, includePrerelease: true , query: "title1", skip: 0, take: 200).ToList();

            // Assert
            var expectedPackages =
                _stageContextMock.Object.PackagesMetadata.Where(
                    p => p.StageKey == DataMockHelper.DefaultStageKey && p.Title.Contains("title1")).ToList();

            filteredPackages.Count.Should().Be(expectedPackages.Count);

            filteredPackages.Should().Equal(expectedPackages, (x, y) => x.Id == y.Id && x.Version == y.Version);
        }

        [Fact]
        public void VerifySearch()
        {
            // Arrange
            _stageContextMock.AddMockStage();
            _stageContextMock.AddMockPackageMetadataList();

            // Act
            var jsonResult = _databaseSearchService.Search("q=title2&skip=1&take=2&prerelease=false");

            // Assert
            var expectedPackages =
                _stageContextMock.Object.PackagesMetadata.Where(p => p.IsPrerelease == false && p.Title.Contains("title2")).OrderBy(x => x.Id).Skip(1).Take(2).ToList();

            var expectedJson =
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    {
                        "data", new JArray
                        {
                            new JObject
                            {
                                {"@id",$"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[0].Id}/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray {"nuget", "nuget2author23"}},
                                {"description", expectedPackages[0].Description},
                                {"iconUrl", expectedPackages[0].IconUrl},
                                {"id", expectedPackages[0].Id},
                                {"licenseUrl", expectedPackages[0].LicenseUrl},
                                {"projectUrl", expectedPackages[0].ProjectUrl},
                                {"registration",$"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[0].Id}/index.json"},
                                {"tags", new JArray {"nuget", "testtag23"}},
                                {"title", expectedPackages[0].Title},
                                {"totalDownloads", 0},
                                {"version", expectedPackages[0].Version},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id",$"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[0].Id}/{expectedPackages[0].Version}.json"},
                                            {"downloads", 0},
                                            {"version", expectedPackages[0].Version}
                                        }
                                    }
                                }
                            },
                            new JObject
                            {
                                {"@id",$"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[1].Id}/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray {"nuget", "nuget2author25"}},
                                {"description", expectedPackages[1].Description},
                                {"iconUrl", expectedPackages[1].IconUrl},
                                {"id", expectedPackages[1].Id},
                                {"licenseUrl", expectedPackages[1].LicenseUrl},
                                {"projectUrl", expectedPackages[1].ProjectUrl},
                                {"registration", $"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[1].Id}/index.json"},
                                {"tags", new JArray {"nuget", "testtag25"}},
                                {"title", expectedPackages[1].Title},
                                {"totalDownloads", 0},
                                {"version", expectedPackages[1].Version},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id",$"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/registration/{expectedPackages[1].Id}/{expectedPackages[1].Version}.json"},
                                            {"downloads", 0},
                                            {"version", expectedPackages[1].Version}
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

            var comparer = JObject.EqualityComparer;
            comparer.Equals(jsonResult, expectedJson).Should().BeTrue();
        }
    }
}
