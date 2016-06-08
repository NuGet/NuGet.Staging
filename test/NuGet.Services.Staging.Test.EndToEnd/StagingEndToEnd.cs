// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NuGet.Client.Staging;
using NuGet.Protocol.Core.v3;
using NuGet.Services.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class StagingEndToEnd
    {
        private const string PackageIdPrefix = "TestPackage";
        private readonly ITestOutputHelper _output;
        private readonly StagingEndToEndConfiguration _configuration;
        private readonly HttpClient _httpClient;
       
        public StagingEndToEnd(ITestOutputHelper output)
        {
            _output = output;
            _configuration = new StagingEndToEndConfiguration();
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task VerifyCommitScenario()
        {
            var client = new StagingClient(new Uri(_configuration.StagingUri), new XUnitLoggerAdapter(_output));

            // Create stage
            var createStageResult = await VerifyCreateStage(client);

            string stageId = createStageResult.Id;

            // Push packages
            var pushedPackages = await VerifyPushPackages(client, stageId, _configuration.PackagesToPushCount);

            // Search packages
            await VerifySearchPackages(client, stageId, pushedPackages);

            // Autocomplete 
            await VerifyAutocompletePackages(client, stageId, pushedPackages);

            // Commit stage
            await VerifyCommitStage(client, stageId, pushedPackages);
        }

        [Fact]
        public async Task VerifyStageManagement()
        {
            var client = new StagingClient(new Uri(_configuration.StagingUri), new XUnitLoggerAdapter(_output));

            // Create stages
            int stagesCount = 2;
            var createdStageResults = new List<StageListView>();

            for (int i = 0; i < stagesCount; i++)
            {
                var createdStageResult = await VerifyCreateStage(client);
                createdStageResults.Add(createdStageResult);
            }

            // Verify list user stages
            await VerifyListUserStages(client, createdStageResults);

            // Drop the stages
            for (int i = 0; i < stagesCount; i++)
            {
                await VerifyDropStage(client, createdStageResults[i].Id);
            }
        }

        private async Task VerifyDropStage(StagingClient client, string stageId)
        {
            _output.WriteLine($"Droping stage {stageId}");

            var dropResult = await client.DropStage(stageId, _configuration.ApiKey);

            dropResult.Id.ShouldBeEquivalentTo(stageId, "Stage Id should be correct");
            dropResult.Status.ShouldBeEquivalentTo("Deleted");
        }

        private async Task VerifyListUserStages(StagingClient client, List<StageListView> createdStageResults)
        {
            var listUserStagesResult = await client.ListUserStages(_configuration.ApiKey);

            foreach (var createdStageResult in createdStageResults)
            {
                var listUserStage = listUserStagesResult.FirstOrDefault(x => x.Id == createdStageResult.Id);

                listUserStage.Should().NotBeNull($"Failed to find a list stage for stage {createdStageResult.Id}");
                listUserStage.Status.ShouldBeEquivalentTo("Active");
                listUserStage.MembershipType.ShouldBeEquivalentTo("Owner");
            }
        }

        private async Task VerifyCommitStage(StagingClient client, string stageId, IReadOnlyList<TestPackage> pushedPackages)
        {
            _output.WriteLine($"Committing stage {stageId}");
            await client.CommitStage(stageId, _configuration.ApiKey);

            Stopwatch commitTime = new Stopwatch();
            commitTime.Start();

            bool commitCompleted = false;

            while (!commitCompleted)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                _output.WriteLine($"Getting commit progress for stage {stageId}");
                var commitProgress = await client.GetCommitProgress(stageId);

                string commitStatus = commitProgress.CommitStatus;
                string errorMessage = commitProgress.ErrorMessage;

                _output.WriteLine($"Commit status: {commitStatus}, Error message: {errorMessage}");

                var packagesProgress = commitProgress.PackageProgressList;

                foreach (var pushedPackage in pushedPackages)
                {
                    var packageProgress = packagesProgress.FirstOrDefault(x => x.Id == pushedPackage.Id);
                    packageProgress.Should().NotBeNull($"Package {pushedPackage.Id} should be shown in stage commit progress.");
                    packageProgress.Version.ShouldBeEquivalentTo(pushedPackage.Version);
                    
                    _output.WriteLine($"Package {packageProgress.Id} {packageProgress.Version} is {packageProgress.Progress}");
                }

                if (commitStatus == Constants.CommitStatus_Failed)
                {
                    Assert.True(false, "Commit failed.");    
                }
                else if (commitStatus == Constants.CommitStatus_Completed)
                {
                    commitCompleted = true;
                }
                else if (commitTime.Elapsed > TimeSpan.FromMinutes(_configuration.CommitTimeoutInMinutes))
                {
                    _output.WriteLine($"Commit of stage {stageId} timed out. Waited for {_configuration.CommitTimeoutInMinutes} minutes.");
                    Assert.True(false, "Commit timed out");
                }
            }
        }

        private async Task<IReadOnlyList<TestPackage>> VerifyPushPackages(StagingClient client, string stageId, int packagesCount)
        {
            var pushedPackages = new List<TestPackage>(packagesCount);

            for (int i = 0; i < packagesCount; i++)
            {
                string packageId = PackageIdPrefix + Guid.NewGuid();

                var package = new TestPackage(packageId).WithDefaultData();
                _output.WriteLine($"Pushing package {i}/{packagesCount}. Stage id: {stageId}, Package: {package.Id} {package.Version}");

                await client.PushPackage(stageId, _configuration.ApiKey, package.Stream);

                pushedPackages.Add(package);
            }

            var stageDetails = await client.GetDetails(stageId);
            stageDetails.PackagesCount.ShouldBeEquivalentTo(packagesCount, "Details should show correct packages count");

            var packages = stageDetails.Packages;

            foreach (var pushedPackage in pushedPackages)
            {
                var packageInDetails = packages.FirstOrDefault(x => x.Id == pushedPackage.Id);
                packageInDetails.Should().NotBeNull($"Package {pushedPackage.Id} should be shown in stage details.");
                packageInDetails.Version.ShouldBeEquivalentTo(pushedPackage.Version);
            }

            return pushedPackages;
        }

        private async Task<StageListView> VerifyCreateStage(StagingClient client)
        {
            var stageName = "TestStage" + Guid.NewGuid();

            _output.WriteLine($"Creating Stage. Name: {stageName}");

            var createStageResult = await client.CreateStage(stageName, _configuration.ApiKey);

            createStageResult.DisplayName.ShouldBeEquivalentTo(stageName, "Display name should be correct");
            createStageResult.Status.ShouldBeEquivalentTo(Constants.StageStatus_Active);
            createStageResult.MembershipType.ShouldBeEquivalentTo(Constants.MembershipType_Owner);

            return createStageResult;
        }

        private async Task VerifySearchPackages(StagingClient client, string stageId, IReadOnlyList<TestPackage> packages)
        {
            const int take = 2;
            const int skip = 1;

            // Act
            var queryResult = await client.Query(stageId, PackageIdPrefix, includePrerelease: true, skip: skip, take: take);

            // Assert
            var expectedPackages = packages.OrderBy(p => p.Id).Skip(skip).Take(take).ToList();

            queryResult[JsonProperties.Data].Should().HaveCount(take);

            await VerifyPackageQueryResult(queryResult[JsonProperties.Data].First, expectedPackages[0]);
            await VerifyPackageQueryResult(queryResult[JsonProperties.Data].Last, expectedPackages[1]);
        }

        private async Task VerifyPackageQueryResult(JToken queryResult, TestPackage package)
        {
            _output.WriteLine($"Verifying query result: {queryResult} against package {package.Id}");

            queryResult[JsonProperties.PackageId].ToString().ShouldBeEquivalentTo(package.Id);
            queryResult[JsonProperties.Description].ToString().ShouldBeEquivalentTo(TestPackage.DefaultDescription);
            queryResult[JsonProperties.IconUrl].ToString().ShouldBeEquivalentTo(TestPackage.DefaultIconUrl);
            queryResult[JsonProperties.LicenseUrl].ToString().ShouldBeEquivalentTo(TestPackage.DefaultLicenseUrl);
            queryResult[JsonProperties.ProjectUrl].ToString().ShouldBeEquivalentTo(TestPackage.DefaultProjectUrl);
            queryResult[JsonProperties.Title].ToString().ShouldBeEquivalentTo(TestPackage.DefaultTitle);
            queryResult[JsonProperties.Version].ToString().ShouldBeEquivalentTo(TestPackage.DefaultVersion);

            queryResult[JsonProperties.Versions].Should().HaveCount(1);
            queryResult[JsonProperties.Versions].First[JsonProperties.Version].ToString().ShouldBeEquivalentTo(TestPackage.DefaultVersion);

            await VerifyUri(new Uri(queryResult[JsonProperties.SubjectId].ToString()));
            await VerifyUri(new Uri(queryResult[Constants.Search_Registration].ToString()));
            await VerifyUri(new Uri(queryResult[JsonProperties.Versions].First[JsonProperties.SubjectId].ToString()));
        }

        private async Task VerifyAutocompletePackages(StagingClient client, string stageId, IReadOnlyList<TestPackage> packages)
        {
            const int take = 2;
            const int skip = 1;

            // Act
            var queryResult = await client.Autocomplete(stageId, query: PackageIdPrefix, packageId: string.Empty, includePrerelease: true, skip: skip, take: take);

            // Assert
            var expectedPackages = packages.Select(p => p.Id).OrderBy(x => x).Skip(skip).Take(take).ToList();

            queryResult[Constants.Autocomplete_TotalHits].ShouldBeEquivalentTo(packages.Count);

            queryResult[JsonProperties.Data].Should().HaveCount(take);
            queryResult[JsonProperties.Data].Select(x => x.ToString()).Should().Equal(expectedPackages);
        }

        private async Task VerifyUri(Uri uri)
        {
            _output.WriteLine($"Verifying Uri: {uri}");

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
