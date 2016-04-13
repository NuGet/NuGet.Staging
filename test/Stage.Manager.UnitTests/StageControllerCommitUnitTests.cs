// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Mvc;
using Moq;
using Newtonsoft.Json;
using Stage.Database.Models;
using Stage.Manager.Controllers;
using Stage.Manager.Filters;
using Stage.Packages;
using Xunit;

namespace Stage.Manager.UnitTests
{
    public class StageControllerCommitUnitTests : StageControllerUnitTests
    {
        [Fact]
        public async Task WhenCommitsCalledAndStageIsCommiting409IsReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            AddMockPackage(stage, "package");
            await _stageController.Commit(stage);

            // Act
            IActionResult actionResult = await _stageController.Commit(stage);

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var result = actionResult as ObjectResult;
            result.StatusCode.Should().Be((int) HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task VerifyOnlyStageOwnerCanCommitStage()
        {
            AttributeHelper.HasServiceFilterAttribute<StageIdFilter>(_stageController, "Commit", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenCommitIsCalledForAnEmptyStage400IsReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");

            // Act
            IActionResult actionResult = await _stageController.Commit(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenCommitIsCalledStageCommitInitiated()
        {
            const string packageId1 = "packageId1";
            const string packageId2 = "packageId2";

            // Arrange
            var stage = await AddMockStage("stage");
            var package1 = AddMockPackage(stage, packageId1);
            var package2 = AddMockPackage(stage, packageId2);

            // Act
            IActionResult actionResult = await _stageController.Commit(stage);

            // Assert

            // Verify return value
            actionResult.Should().BeOfType<HttpStatusCodeResult>();
            var result = actionResult as HttpStatusCodeResult;
            result.StatusCode.Should().Be((int)HttpStatusCode.Created);

            // Verify the pushed data
            _packageServiceMock.Verify(x => x.PushBatchAsync(It.IsAny<PackageBatchPushData>()), Times.Once);
            _pushedBatches.Count.Should().Be(1);
            var pushedBatch = _pushedBatches.First();
            pushedBatch.StageId.Should().Be(stage.Id);
            pushedBatch.PackagePushDataList.Count.Should().Be(2);
            VerifyPackagePush(pushedBatch.PackagePushDataList.First(x => x.Id == package1.Id), package1);
            VerifyPackagePush(pushedBatch.PackagePushDataList.First(x => x.Id == package2.Id), package2);

            // Verify tracking id was saved
            stage.Commits.First().TrackId.Should().Be(TrackId);
            stage.Commits.First().Status.Should().Be(CommitStatus.Pending);
        }

        [Fact]
        public void VerifyGetCommitProgressIsAnonymous()
        {
            AuthorizationTest.IsAnonymous(_stageController, "GetCommitProgress", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenGetCommitProgressIsCalledAndStageNotCommited400Returned()
        {
            // Arrange
            var stage = await AddMockStage("stage");

            // Act
            IActionResult actionResult = _stageController.GetCommitProgress(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task WhenGetCommitProgressIsCalledAndStageHasMultipleCommitsLatestReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            var commit1 = AddMockCommit(stage, DateTime.UtcNow);
            var commit2 = AddMockCommit(stage, DateTime.UtcNow + TimeSpan.FromMinutes(10));
            commit1.Status = CommitStatus.Failed;

            // Act
            IActionResult actionResult = _stageController.GetCommitProgress(stage);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();
            var result = actionResult as HttpOkObjectResult;
            result.Value.Should().BeOfType<ViewStageCommitProgress>();
            var progress = result.Value as ViewStageCommitProgress;
            progress.CommitStatus.Should().Be(commit2.Status.ToString());
        }

        [Fact]
        public async Task WhenGetCommitProgressIsCalledAndCommitProgressNotReportedSucceed()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            AddMockPackage(stage, "package1");
            AddMockPackage(stage, "package2");

            await _stageController.Commit(stage); 

            // Act
            IActionResult actionResult = _stageController.GetCommitProgress(stage);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();
            var result = actionResult as HttpOkObjectResult;
            result.Value.Should().BeOfType<ViewStageCommitProgress>();
            var progress = result.Value as ViewStageCommitProgress;

            VerifyCommitProgress(progress, stage);
        }

        [Fact]
        public async Task WhenGetCommitProgressIsCalledAndCommitProgressReportedSucceed()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            var package1 = AddMockPackage(stage, "package1");
            var package2 = AddMockPackage(stage, "package2");

            await _stageController.Commit(stage);
            var progressReport = new BatchPushProgressReport
            {
                Status = PushProgressStatus.InProgress,
                PackagePushProgressReports = new List<PackagePushProgressReport>
                {
                   new PackagePushProgressReport
                   {
                       Id = package1.Id,
                       Version = package1.Version,
                       Status = PushProgressStatus.InProgress
                   },
                   new PackagePushProgressReport
                   {
                       Id = package2.Id,
                       Version = package2.Version,
                       Status = PushProgressStatus.Pending
                   }
                },
                FailureDetails = "None"
            };

            stage.Commits.First().Progress = JsonConvert.SerializeObject(progressReport);
            stage.Commits.First().Status = CommitStatus.InProgress;

            // Act
            IActionResult actionResult = _stageController.GetCommitProgress(stage);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();
            var result = actionResult as HttpOkObjectResult;
            result.Value.Should().BeOfType<ViewStageCommitProgress>();
            var progress = result.Value as ViewStageCommitProgress;

            VerifyCommitProgress(progress, stage);
        }

        private void VerifyCommitProgress(ViewStageCommitProgress actual, Database.Models.Stage expected)
        {
            VerifyViewStage(actual, expected);

            var commit = expected.Commits.First();
            var progress = _stageServiceMock.Object.GetCommitProgress(commit);

            actual.CommitStatus.Should().Be(commit.Status.ToString());
            actual.PackageProgressList.Count.Should().Be(expected.Packages.Count);

            foreach (var package in expected.Packages)
            {
                var packageView = actual.PackageProgressList.Find(x => x.Id == package.Id && x.Version == package.Version);
                Assert.NotNull(packageView);

                if (progress != null)
                {
                    var packageProgress = progress.PackagePushProgressReports.Find(x => x.Id == package.Id && x.Version == package.Version);
                    packageView.Progress.Should().Be(packageProgress.Status.ToString());
                }
                else
                {
                    packageView.Progress.Should().Be(CommitStatus.Pending.ToString());
                }
            }

            if (progress != null)
            {
                actual.ErrorMessage.Should().Be(progress.FailureDetails);
            }
        }

        private StageCommit AddMockCommit(Database.Models.Stage stage, DateTime requestTime)
        {
            var commit = new StageCommit
            {
                RequestTime = requestTime,
                TrackId = TrackId,
                Status = CommitStatus.Pending
            };

            stage.Commits.Add(commit);
            return commit;
        }
    }
}