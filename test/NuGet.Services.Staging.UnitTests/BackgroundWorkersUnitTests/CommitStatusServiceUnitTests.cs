// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager.UnitTests;
using NuGet.Services.Staging.PackageService;
using Xunit;

namespace NuGet.Services.Staging.UnitTests.BackgroundWorkersUnitTests
{
    public class CommitStatusServiceUnitTests
    {
        private readonly StageContextMock _stageContextMock;

        public CommitStatusServiceUnitTests()
        {
            _stageContextMock = new StageContextMock();
        }

        [Theory]
        [InlineData(PushProgressStatus.Pending, CommitStatus.Pending)]
        [InlineData(PushProgressStatus.Completed, CommitStatus.Completed)]
        [InlineData(PushProgressStatus.Failed, CommitStatus.Failed)]
        [InlineData(PushProgressStatus.InProgress, CommitStatus.InProgress)]
        public async Task WhenUpdateProgressIsCalledCommitIsUpdated(PushProgressStatus progressStatus, CommitStatus expectedCommitStatus)
        {
            using (var commitStatusService = new CommitStatusService(_stageContextMock.Object))
            {
                // Arrange
                var commit = new StageCommit
                {
                    StageKey = 1,
                    Status = CommitStatus.Pending,
                    LastProgressUpdate = DateTime.MinValue
                };

                var stage = new Database.Models.Stage
                {
                    Key = commit.StageKey
                };

                _stageContextMock.Object.Stages.Add(stage);

                var report = new BatchPushProgressReport
                {
                    Status = progressStatus
                };

                // Act
                await commitStatusService.UpdateProgress(commit, report);

                // Assert
                commit.Status.ShouldBeEquivalentTo(expectedCommitStatus);
                commit.Progress.Should().NotBeEmpty();
                commit.LastProgressUpdate.Should().NotBe(DateTime.MinValue);

                if (progressStatus == PushProgressStatus.Completed)
                {
                    stage.Status.ShouldBeEquivalentTo(StageStatus.Committed);
                }

                if (progressStatus == PushProgressStatus.Failed)
                {
                    stage.Status.ShouldBeEquivalentTo(StageStatus.Active);
                }
            }

            _stageContextMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}