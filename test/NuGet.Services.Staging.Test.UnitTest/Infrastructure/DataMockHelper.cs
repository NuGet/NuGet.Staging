// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Test.Common;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public static class DataMockHelper
    {
        public static UserInformation DefaultUser = new UserInformation { UserKey = 2, UserName = "testUser" };
        public const int DefaultStageKey = 1;

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
                Id = Guid.NewGuid().ToString(),
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
                PackageMetadata = new PackageMetadata
                {
                    Authors = TestPackage.DefaultAuthors,
                    Description = TestPackage.DefaultDescription,
                    IconUrl = TestPackage.DefaultIconUrl,
                    Id = packageId,
                    LicenseUrl = TestPackage.DefaultLicenseUrl,
                    Version = version,
                    Owners = TestPackage.DefaultOwners,
                    ProjectUrl = TestPackage.DefaultProjectUrl,
                    Tags = TestPackage.DefaultTags,
                    Summary = TestPackage.DefaultSummary,
                    Title = TestPackage.DefaultTitle,
                    StageKey = stage.Key,
                    IsPrerelease = true
                }
            };

            stage.Packages.Add(package);
            stageContextMock.Object.PackagesMetadata.Add(package.PackageMetadata);

            return package;
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
