// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Test.Common;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public static class DataMockHelper
    {
        public static UserInformation DefaultUser = new UserInformation { UserKey = 2, UserName = "testUser" };
        public const int DefaultStageKey = 1;
        public const string DefaultStageId = "94bdc785-617f-4335-83c0-f80d88c01cc7";

        public static Stage AddMockStage(this StageContextMock stageContextMock)
        {
            var member = new StageMembership
            {
                Key = 1,
                MembershipType = MembershipType.Owner,
                StageKey = DefaultStageKey,
                UserKey = DefaultUser.UserKey
            };

            var stage = new Stage
            {
                Key = DefaultStageKey,
                Id = DefaultStageId,
                DisplayName = "DefaultStage",
                Memberships = new List<StageMembership> { member },
                Packages = new List<StagedPackage>()
            };

            stageContextMock.Object.Stages.Add(stage);
            stageContextMock.Object.StageMemberships.Add(member);

            return stage;
        }

        public static StagedPackage AddMockPackage(this StageContextMock stageContextMock, Stage stage, string packageId)
        {
            const string version = "1.0.0";
            var package = new StagedPackage
            {
                Id = packageId,
                Version = version,
                NormalizedVersion = version,
                NupkgUrl = $"http://api.nuget.org/{stage.Id}/{packageId}/{version}/{packageId}.{version}.nupkg",
                UserKey = DefaultUser.UserKey,
                PackageMetadata = CreateDefaultPackageMetadata(packageId, version, stage.Key)
            };

            stage.Packages.Add(package);
            stageContextMock.Object.PackagesMetadata.Add(package.PackageMetadata);

            return package;
        }

        public static IEnumerable<PackageMetadata> AddMockPackageMetadataList(this StageContextMock stageContextMock, int stageKey = DefaultStageKey)
        {
            var packageMetadataList = Enumerable.Range(0, 100).Select(i =>
            {
                var packageMetadata = CreateDefaultPackageMetadata(TestPackage.DefaultId + i, $"{i}.0.0");
                packageMetadata.Description += i;
                packageMetadata.Authors += "author" + i;
                packageMetadata.Tags += "tag" + i;
                packageMetadata.Title += i;
                packageMetadata.IsPrerelease = i % 2 == 0;
                packageMetadata.StageKey = stageKey;

                return packageMetadata;
            }).ToList();

            stageContextMock.Object.PackagesMetadata.AddRange(packageMetadataList);
            return packageMetadataList;
        } 

        public static PackageMetadata CreateDefaultPackageMetadata(
            string id = TestPackage.DefaultId,
            string version = TestPackage.DefaultVersion,
            int stageKey = DefaultStageKey)
        {
            return new PackageMetadata
            {
                Authors = TestPackage.DefaultAuthors,
                Description = TestPackage.DefaultDescription,
                IconUrl = TestPackage.DefaultIconUrl,
                Id = id,
                LicenseUrl = TestPackage.DefaultLicenseUrl,
                Version = version,
                Owners = TestPackage.DefaultOwners,
                ProjectUrl = TestPackage.DefaultProjectUrl,
                Tags = TestPackage.DefaultTags,
                Summary = TestPackage.DefaultSummary,
                Title = TestPackage.DefaultTitle,
                IsPrerelease = true,
                StageKey = stageKey
            };
        }
    }
}
