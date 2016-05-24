// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NuGet.Services.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class StagingEndToEndConfiguration
    {
        public string ApiKey { get; set; }
        public int PackagesToPushCount { get; set; }
        public int CommitTimeoutInMinutes { get; set; }
        public string StagingUri { get; set; }

        public StagingEndToEndConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("testsettings.json");

            var configurationRoot = builder.Build();

            configurationRoot.GetSection("StagingEndToEnd").Bind(this);
        }
    }

    public class StagingEndToEnd
    {
        private readonly ITestOutputHelper _output;
        private readonly StagingEndToEndConfiguration _configuration;
       
        public StagingEndToEnd(ITestOutputHelper output)
        {
            _output = output;
            _configuration = new StagingEndToEndConfiguration();
        }

        [Fact]
        public async Task VerifyCommitScenario()
        {
            var client = new StagingClient(new Uri(_configuration.StagingUri), _output);

            // Create stage
            var createStageResult = await VerifyCreateStage(client);

            string stageId = createStageResult[Constants.Stage_Id].ToString();

            // Push packages
            var pushedPackages = await VerifyPushPackages(client, stageId, _configuration.PackagesToPushCount);

            // Commit stage
            await VerifyCommitStage(client, stageId, pushedPackages);
        }

        [Fact]
        public async Task VerifyStageManagement()
        {
            var client = new StagingClient(new Uri(_configuration.StagingUri), _output);

            // Create stages
            int stagesCount = 2;
            var createdStageResults = new List<JObject>();

            for (int i = 0; i < stagesCount; i++)
            {
                var createdStageResult = await VerifyCreateStage(client);
                createdStageResults.Add(createdStageResult);
            }

            // Verify list user stages
            await VerifyListUserStages(client, stagesCount, createdStageResults);

            // Drop the stages
            for (int i = 0; i < stagesCount; i++)
            {
                await VerifyDropStage(client, createdStageResults[i][Constants.Stage_Id].ToString());
            }
        }

        private async Task VerifyDropStage(StagingClient client, string stageId)
        {
            _output.WriteLine($"Droping stage {stageId}");

            var dropResult = await client.DropStage(stageId, _configuration.ApiKey);

            dropResult[Constants.Stage_Id].ShouldAllBeEquivalentTo(stageId, "Stage Id should be correct");
            dropResult[Constants.Stage_Status].ShouldAllBeEquivalentTo("Deleted");
        }

        private async Task VerifyListUserStages(StagingClient client, int stagesCount, List<JObject> createdStageResults)
        {
            var listUserStagesResult = await client.ListUserStages(_configuration.ApiKey);

            foreach (var createdStageResult in createdStageResults)
            {
                var listUserStage = listUserStagesResult
                    .First(x => x[Constants.Stage_Id].ToString() == createdStageResult[Constants.Stage_Id].ToString());

                listUserStage.Should().NotBeNull($"Failed to find a list stage for stage {createdStageResult[Constants.Stage_Id]}");
                listUserStage[Constants.Stage_Status].ToString().ShouldBeEquivalentTo("Active");
                listUserStage[Constants.Stage_MembershipType].ToString().ShouldBeEquivalentTo("Owner");
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

                string commitStatus = commitProgress[Constants.CommitProgress_CommitStatus].ToString();
                string errorMessage = commitProgress[Constants.CommitProgress_ErrorMessage].ToString();

                _output.WriteLine($"Commit status: {commitStatus}, Error message: {errorMessage}");

                var packagesProgress = commitProgress[Constants.CommitProgress_PackageProgressList];

                foreach (var pushedPackage in pushedPackages)
                {
                    var packageProgress = packagesProgress.FirstOrDefault(x => x[Constants.Stage_Id].ToString() == pushedPackage.Id);
                    packageProgress.Should().NotBeNull($"Package {pushedPackage.Id} should be shown in stage commit progress.");
                    packageProgress[Constants.Package_Version].ToString().ShouldBeEquivalentTo(pushedPackage.Version);
                    
                    _output.WriteLine($"Package {packageProgress[Constants.Stage_Id]} {packageProgress[Constants.Package_Version]} is {packageProgress[Constants.Package_Progress]}");
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
            string version = "1.0.0";

            for (int i = 0; i < packagesCount; i++)
            {
                string packageId = "TestPackage" + Guid.NewGuid();

                var package = new TestPackage(packageId, version).WithDefaultData();
                _output.WriteLine($"Pushing package {i}/{packagesCount}. Stage id: {stageId}, Package: {package.Id} {package.Version}");

                await client.PushPackage(stageId, _configuration.ApiKey, package.Stream);

                pushedPackages.Add(package);
            }

            var stageDetails = await client.GetDetails(stageId);
            stageDetails[Constants.Stage_PackageCount].Value<int>()
                .ShouldBeEquivalentTo(packagesCount, "Details should show correct packages count");

            var packages = stageDetails[Constants.Stage_Packages];

            foreach (var pushedPackage in pushedPackages)
            {
                var packageInDetails = packages.FirstOrDefault(x => x[Constants.Stage_Id].ToString() == pushedPackage.Id);
                packageInDetails.Should().NotBeNull($"Package {pushedPackage.Id} should be shown in stage details.");
                packageInDetails[Constants.Package_Version].ToString().ShouldBeEquivalentTo(pushedPackage.Version);
            }

            return pushedPackages;
        }

        private async Task<JObject> VerifyCreateStage(StagingClient client)
        {
            // TODO: make this long once new version is deployed
            var stageName = ("TestStage" + Guid.NewGuid()).Substring(0, 32);

            _output.WriteLine($"Creating Stage. Name: {stageName}");

            var createStageResult = await client.CreateStage(stageName, _configuration.ApiKey);

            createStageResult[Constants.Stage_DisplayName].ToString().ShouldBeEquivalentTo(stageName, "Display name should be correct");
            createStageResult[Constants.Stage_Status].ToString().ShouldBeEquivalentTo(Constants.StageStatus_Active);
            createStageResult[Constants.Stage_MembershipType].ToString().ShouldBeEquivalentTo(Constants.MembershipType_Owner);

            return createStageResult;
        }
    }
}
