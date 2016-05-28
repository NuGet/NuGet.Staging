// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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

        /*tests:
         * 2. check all special fields
         * 5. empty results works
         * 6. parsing of query string is correct
         * 7. Check or (id:x id:y), think of more
         * 8. Filter by stage works
         * 9. simple query works
         */

        public static IEnumerable<object[]> _queryVerificationTestInput = new List<object[]>
        {
            new object[] // Test take + empty query
            {
                true, "", 0, 10, new PackageFilter(allPackages => allPackages.Take(10))
            },
            new object[] // Test skip
            {
                true, "", 15, 200, new PackageFilter(allPackages => allPackages.Skip(15))
            },
            new object[] // Test prerelease
            {
                false, "", 0, 200, new PackageFilter(allPackages => allPackages.Where(p => !p.IsPrerelease))
            },
            new object[] // Test packageid field
            {
                true, QueryField.PackageId +":json5", 0, 10, new PackageFilter(allPackages => allPackages.Where(p => p.Id == "json5"))
            },
            new object[] // Test id field
            {
                true, QueryField.Id +":json5", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Id.Contains("json5")))
            },
            new object[] // Test version field
            {
                true, QueryField.Version +":2", 0, 100, new PackageFilter(allPackages => allPackages.Where(p => p.Version == "2.0.0"))
            },
            new object[] // Test illegal version field
            {
                true, QueryField.Version +":abc", 0, 100, new PackageFilter(allPackages => new List<PackageMetadata>())
            }
        }; 

        public DatabaseSearchUnitTests()
        {
            _stageContextMock = new StageContextMock();
            var pathCalculator = new V3PathCalculator(new Uri("http://api.nuget.org"));
            _databaseSearchService = new DatabaseSearchService(_stageContextMock.Object, pathCalculator, DefaultStageId);    
        }

        [Theory]
        [MemberData("_queryVerificationTestInput")]
        public void VerifyPackageFilteringLogic(bool includePrerelease, string query, int skip, int take, PackageFilter expectedResultCalculator)
        {
            // Arrange
            var allPackages = GeneratePackageList();
            _stageContextMock.Object.PackagesMetadata.AddRange(allPackages);

            // Act
            var filteredPackages = _databaseSearchService.ApplyQueryParameters(DefaultStageKey, includePrerelease, query, skip, take).ToList();

            // Assert
            var expectedPackages = expectedResultCalculator(allPackages).ToList();

            filteredPackages.Count.Should().Be(expectedPackages.Count, "Number of packages should be as expected");

            filteredPackages.Except(expectedPackages, new PackageMetadataComparer()).Should().BeEmpty("Lists should be identical");
        }

        private IEnumerable<PackageMetadata> GeneratePackageList()
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
                packageMetadata.StageKey = DefaultStageKey;

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
