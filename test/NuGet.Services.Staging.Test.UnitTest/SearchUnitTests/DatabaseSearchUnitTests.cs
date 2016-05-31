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
using NuGet.Services.Test.Common;
using NuGet.Services.V3Repository;
using Xunit;

using PackageFilter = System.Func<System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>, System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>>;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public class DatabaseSearchUnitTests
    {
        private readonly DatabaseSearchService _databaseSearchService;
        private readonly StageContextMock _stageContextMock;
        private const string DefaultStageId = "94bdc785-617f-4335-83c0-f80d88c01cc7";
        private const int DefaultStageKey = 22;

        public static IEnumerable<object[]> _queryVerificationTestInput = new List<object[]>
        {
             new object[]
            {
                "Test simple query", true, "title1", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Title.Contains("title1")))
            },
            new object[]
            {
                "Test take + empty query", true, "", 0, 10, new PackageFilter(allPackages => allPackages.Take(10))
            },
            new object[]
            {
                "Test skip", true, "", 15, 200, new PackageFilter(allPackages => allPackages.Skip(15))
            },
            new object[]
            {
                "Test prerelease", false, "", 0, 200, new PackageFilter(allPackages => allPackages.Where(p => !p.IsPrerelease))
            },
            new object[]
            {
                "Test packageid field", true, QueryField.PackageId +":json5", 0, 10, new PackageFilter(allPackages => allPackages.Where(p => p.Id == "json5"))
            },
            new object[]
            {
                "Test id field", true, QueryField.Id +":json5", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Id.Contains("json5")))
            },
            new object[]
            {
                "Test version field", true, QueryField.Version +":2", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Version == "2.0.0"))
            },
            new object[]
            {
                "Test illegal version field", true, QueryField.Version +":abc", 0, 100, new PackageFilter(allPackages => new List<PackageMetadata>())
            },
            new object[]
            {
                "Test title field", true, QueryField.Title +":title2", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Title.Contains("title2")))
            },
            new object[]
            {
                "Test description field", true, QueryField.Description +":NuGet4", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Description.Contains("NuGet4")))
            },
            new object[]
            {
                "Test tag field", true, QueryField.Tag +":test5", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Tags.Contains("test5")))
            },
            new object[]
            {
                "Test author field", true, QueryField.Author +":nuget27", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27")))
            },
            new object[]
            {
                "Test summary field", true, QueryField.Summary +":nuget6", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Summary.Contains("nuget6")))
            },
            new object[]
            {
                "Test owner field", true, QueryField.Owner +":owners9", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Owners.Contains("owners9")))
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
                new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27") || p.Owners.Contains("owners9")))
            },
            new object[]
            {
                "Test search with field and text",
                true,
                $"owners9 {QueryField.Author}:nuget27",
                0,
                100,
                new PackageFilter(allPackages => allPackages.Where(p => p.Authors.Contains("nuget27") || p.Owners.Contains("owners9")))
            }
        }; 

        public DatabaseSearchUnitTests()
        {
            _stageContextMock = new StageContextMock();
            var pathCalculator = new V3PathCalculator(new Uri($"http://api.nuget.org/stage/{DefaultStageId}/"));
            _databaseSearchService = new DatabaseSearchService(_stageContextMock.Object, pathCalculator, DefaultStageId);    
        }

        [Theory]
        [MemberData("_queryVerificationTestInput")]
        public void VerifyApplyQueryParameters(string testName, bool includePrerelease, string query, int skip, int take, PackageFilter expectedResultCalculator)
        {
            // Arrange
            var allPackages = GeneratePackageList();
            _stageContextMock.Object.PackagesMetadata.AddRange(allPackages);

            // Act
            var filteredPackages = _databaseSearchService.ApplyQueryParameters(DefaultStageKey, includePrerelease, query, skip, take).ToList();

            // Assert
            var expectedPackages = expectedResultCalculator(allPackages).ToList();

            filteredPackages.Count.Should().Be(expectedPackages.Count, $"for test {testName} count should be the same");

            filteredPackages.Except(expectedPackages, new PackageMetadataComparer()).Should().BeEmpty($"for test {testName} lists should be identical");
        }

        [Fact]
        public void VerifyStageFiltering()
        {
            // Arrange
            var stage1Packages = GeneratePackageList(stageKey: DefaultStageKey);
            var stage2Packages = GeneratePackageList(stageKey: DefaultStageKey + 1);

            _stageContextMock.Object.PackagesMetadata.AddRange(stage2Packages);
            _stageContextMock.Object.PackagesMetadata.AddRange(stage1Packages);

            // Act
            var filteredPackages = _databaseSearchService.ApplyQueryParameters(DefaultStageKey, includePrerelease: true , query: "title1", skip: 0, take: 200).ToList();

            // Assert
            var expectedPackages =
                _stageContextMock.Object.PackagesMetadata.Where(
                    p => p.StageKey == DefaultStageKey && p.Title.Contains("title1")).ToList();

            filteredPackages.Count.Should().Be(expectedPackages.Count);

            filteredPackages.Except(expectedPackages, new PackageMetadataComparer()).Should().BeEmpty();
        }

        [Fact]
        public void VerifyQueryParsing()
        {
            // Arrange
            var stage = _stageContextMock.AddMockStage();
            stage.Id = DefaultStageId;

            var allPackages = GeneratePackageList(stage.Key);
            _stageContextMock.Object.PackagesMetadata.AddRange(allPackages);

            string query = "q=title2&skip=1&take=2&prerelease=false";

            // Act
            var jsonResult = _databaseSearchService.Search(query);

            // Assert
            var expectedPackages =
                _stageContextMock.Object.PackagesMetadata.Where(p => p.IsPrerelease == false && p.Title.Contains("title2")).Skip(1).Take(2).ToList();

            var expectedJson =
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    {
                        "data", new JArray
                        {
                            new JObject
                            {
                                {"@id",$"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[0].Id}/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray {"nuget", "nuget2author23"}},
                                {"description", expectedPackages[0].Description},
                                {"iconUrl", expectedPackages[0].IconUrl},
                                {"id", expectedPackages[0].Id},
                                {"licenseUrl", expectedPackages[0].LicenseUrl},
                                {"projectUrl", expectedPackages[0].ProjectUrl},
                                {"registration",$"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[0].Id}/index.json"},
                                {"tags", new JArray {"nuget", "testtag23"}},
                                {"title", expectedPackages[0].Title},
                                {"totalDownloads", 0},
                                {"version", expectedPackages[0].Version},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id",$"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[0].Id}/{expectedPackages[0].Version}.json"},
                                            {"downloads", 0},
                                            {"version", expectedPackages[0].Version}
                                        }
                                    }
                                }
                            },
                            new JObject
                            {
                                {"@id",$"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[1].Id}/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray {"nuget", "nuget2author25"}},
                                {"description", expectedPackages[1].Description},
                                {"iconUrl", expectedPackages[1].IconUrl},
                                {"id", expectedPackages[1].Id},
                                {"licenseUrl", expectedPackages[1].LicenseUrl},
                                {"projectUrl", expectedPackages[1].ProjectUrl},
                                {"registration", $"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[1].Id}/index.json"},
                                {"tags", new JArray {"nuget", "testtag25"}},
                                {"title", expectedPackages[1].Title},
                                {"totalDownloads", 0},
                                {"version", expectedPackages[1].Version},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id",$"http://api.nuget.org/stage/{DefaultStageId}/registration/{expectedPackages[1].Id}/{expectedPackages[1].Version}.json"},
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

        private IEnumerable<PackageMetadata> GeneratePackageList(int stageKey = DefaultStageKey)
        {
            return Enumerable.Range(0, 100).Select(i =>
            {
                var packageMetadata = DataMockHelper.CreateDefaultPackageMetadata(TestPackage.DefaultId + i,
                    $"{i}.0.0");
                packageMetadata.Description += i;
                packageMetadata.Authors += "author" + i;
                packageMetadata.Tags += "tag" + i;
                packageMetadata.Title += i;
                packageMetadata.IsPrerelease = i%2 == 0;
                packageMetadata.StageKey = stageKey;

                return packageMetadata;
            });
        }

        private class PackageMetadataComparer : IEqualityComparer<PackageMetadata>
        {
            public bool Equals(PackageMetadata x, PackageMetadata y)
            {
                // Check whether the compared objects reference the same data. 
                if (Object.ReferenceEquals(x, y)) return true;

                // Check whether any of the compared objects is null. 
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                return x.Id == y.Id && x.Version == y.Version;
            }

            public int GetHashCode(PackageMetadata package)
            {
                // Check whether the object is null. 
                if (Object.ReferenceEquals(package, null)) return 0;

                int hashPackageId = package.Id == null ? 0 : package.Id.GetHashCode();

                int hashPackageVersion = package.Version.GetHashCode();

                // Calculate the hash code for the product. 
                return hashPackageVersion ^ hashPackageId;
            }
        }
    }
}
