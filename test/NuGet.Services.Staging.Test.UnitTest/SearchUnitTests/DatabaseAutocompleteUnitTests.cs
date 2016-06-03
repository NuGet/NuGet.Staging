// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Search;
using NuGet.Services.V3Repository;
using Xunit;
using AutocompleteResultGenerator = System.Func<System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>, System.Collections.Generic.IEnumerable<string>>;
using AutocompleteTotalHitsCalculator = System.Func<System.Collections.Generic.IEnumerable<NuGet.Services.Staging.Database.Models.PackageMetadata>, int>;

namespace NuGet.Services.Staging.Test.UnitTest.SearchUnitTests
{
    public class DatabaseAutocompleteUnitTests
    {
        private readonly DatabaseSearchService _databaseSearchService;
        private readonly StageContextMock _stageContextMock;

        public static IEnumerable<object[]> _autocompleteInternalTestInput = new List<object[]>
        {
            new object[]
            {
                "When q and id are empty q is chosen",
                true,
                "",
                "",
                0,
                1000,
                new AutocompleteResultGenerator(packages => packages.Select(p => p.Id).Distinct().OrderBy(x => x)),
                new AutocompleteTotalHitsCalculator(packages => packages.Select(p => p.Id).Distinct().Count())
            },
            new object[]
            {
                "When q and id are not empty q is chosen",
                true,
                "json",
                "jquery",
                0,
                1000,
                new AutocompleteResultGenerator(packages => packages.Where(p => p.Id.Contains("json")).Select(p => p.Id).Distinct().OrderBy(x => x)),
                new AutocompleteTotalHitsCalculator(packages => packages.Where(p => p.Id.Contains("json")).Select(p => p.Id).Distinct().Count())
            },
            new object[]
            {
                "When q is not empty and id is empty q is chosen",
                true,
                "json",
                "",
                0,
                1000,
                new AutocompleteResultGenerator(packages => packages.Where(p => p.Id.Contains("json")).Select(p => p.Id).Distinct().OrderBy(x => x)),
                new AutocompleteTotalHitsCalculator(packages => packages.Where(p => p.Id.Contains("json")).Select(p => p.Id).Distinct().Count())
            },
            new object[]
            {
                "When id is not empty and q is empty id is chosen",
                true,
                "",
                "json1",
                0,
                1000,
                new AutocompleteResultGenerator(packages => packages.Where(p => p.Id == "json1").Select(p => p.Version).OrderBy(x => x)),
                new AutocompleteTotalHitsCalculator(packages => packages.Count(p => p.Id == "json1"))
            },
            new object[]
            {
                "Test empty results",
                true,
                "",
                "json111",
                0,
                1000,
                new AutocompleteResultGenerator(packages => new List<string>()),
                new AutocompleteTotalHitsCalculator(packages => 0)
            },
            new object[]
            {
                "Test includePrerelease",
                false,
                "",
                "",
                0,
                1000,
                new AutocompleteResultGenerator(packages => packages.Where(p => !p.IsPrerelease).Select(p => p.Id).Distinct().OrderBy(x => x)),
                new AutocompleteTotalHitsCalculator(packages => packages.Where(p => !p.IsPrerelease).Select(p => p.Id).Distinct().Count())
            },
            new object[]
            {
                "Test skip and take for q",
                true,
                "json1",
                "",
                5,
                3,
                new AutocompleteResultGenerator(packages => packages.Where(p => p.Id.Contains("json1")).Select(p => p.Id).Distinct().OrderBy(x => x).Skip(5).Take(3)),
                new AutocompleteTotalHitsCalculator(packages => packages.Where(p => p.Id.Contains("json1")).Select(p => p.Id).Distinct().Count())
            }
            ,
            new object[]
            {
                "Test skip and take for id",
                true,
                "",
                "json1",
                2,
                2,
                new AutocompleteResultGenerator(packages => packages.Where(p => p.Id == "json1").Select(p => p.Version).Distinct().OrderBy(x => x).Skip(2).Take(2)),
                new AutocompleteTotalHitsCalculator(packages => packages.Count(p => p.Id == "json1"))
            }
        };

        public DatabaseAutocompleteUnitTests()
        {
            _stageContextMock = new StageContextMock();
            var pathGenerator = new V3PathGenerator(new Uri($"http://api.nuget.org/stage/{DataMockHelper.DefaultStageId}/"));
            _databaseSearchService = new DatabaseSearchService(_stageContextMock.Object, pathGenerator, DataMockHelper.DefaultStageId);
        }

        [Theory]
        [MemberData("_autocompleteInternalTestInput")]
        public void TestAutocompleteInternal(string testName, bool includePrerelease, string q, string id, int skip, int take,
                                             AutocompleteResultGenerator expectedResultGenerator, AutocompleteTotalHitsCalculator expectedTotalHitsCalculator)
        {
            // Arrange
            var allPackages = GenerateMockInput();

            // Act
            int totalHits;
            var itemsList = _databaseSearchService.AutocompleteInternal(DataMockHelper.DefaultStageKey, includePrerelease, q, id, skip, take, out totalHits).ToList();

            // Assert
            int expectedTotalHits = expectedTotalHitsCalculator(allPackages);
            totalHits.ShouldBeEquivalentTo(expectedTotalHits, testName);

            var expectedItems = expectedResultGenerator(allPackages).ToList();

            itemsList.Count.Should().Be(expectedItems.Count, testName);
            itemsList.Should().Equal(expectedItems, testName);
        }

        [Fact]
        public void VerifyAutocompleteInternalFilteringByStage()
        {
            // Arrange
            _stageContextMock.AddMockPackageMetadataList(stageKey: DataMockHelper.DefaultStageKey);
            _stageContextMock.AddMockPackageMetadataList(stageKey: DataMockHelper.DefaultStageKey + 1);

            // Act
            int totalHits;
            var itemsList = _databaseSearchService.AutocompleteInternal(
                DataMockHelper.DefaultStageKey,
                includePrerelease: true,
                q: "json2",
                id: string.Empty,
                skip: 5,
                take: 7,
                totalHits: out totalHits).ToList();

            // Assert
            int expectedTotalHits = _stageContextMock.Object.PackagesMetadata.Count(x => x.StageKey == DataMockHelper.DefaultStageKey && x.Id.Contains("json2"));
            totalHits.ShouldBeEquivalentTo(expectedTotalHits);

            var expectedItems = _stageContextMock.Object.PackagesMetadata
                .Where(x => x.StageKey == DataMockHelper.DefaultStageKey && x.Id.Contains("json2"))
                .Select(x =>x.Id)
                .Skip(5)
                .Take(7)
                .ToList();

            itemsList.Count.Should().Be(expectedItems.Count);
            itemsList.Should().Equal(expectedItems);
        }

        [Fact]
        public void VerifyAutocomplete()
        {
            // Arrange
            _stageContextMock.AddMockStage();
            GenerateMockInput();

            // Act
            var jsonResult = _databaseSearchService.Autocomplete("q=json5&skip=1&take=2&prerelease=false");

            // Assert
            var expectedItems =
                _stageContextMock.Object.PackagesMetadata
                .Where(p => p.IsPrerelease == false && p.Id.Contains("json5"))
                .Select(x => x.Id).Distinct()
                .OrderBy(x => x)
                .Skip(1)
                .Take(2)
                .ToList();

            int totalHits = 
                _stageContextMock.Object.PackagesMetadata
                .Where(p => p.IsPrerelease == false && p.Id.Contains("json5"))
                .Select(x => x.Id)
                .Distinct()
                .Count();

            var expectedJson =
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    { "totalHits", totalHits },
                    { "index", "DB" },
                    {
                        "data", new JArray(expectedItems)
                    }
                };

            // Verify lastReopen exists, and remove it because we can't mock DateTime.UtcNow
            jsonResult["lastReopen"].Should().NotBeNull();
            jsonResult.Remove("lastReopen");
            var comparer = JObject.EqualityComparer;
            comparer.Equals(jsonResult, expectedJson).Should().BeTrue();
        }
    
        private List<PackageMetadata> GenerateMockInput()
        {
            // Generate 5 versions for each package
            for (int i = 0; i < 5; i++)
            {
                var packagesList = _stageContextMock.AddMockPackageMetadataList().ToList();
                foreach (var packageMetadata in packagesList)
                {
                    packageMetadata.Version = $"{i}.0.0";
                }
            }

            return _stageContextMock.Object.PackagesMetadata.ToList();
        }
    }
}
