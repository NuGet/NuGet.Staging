// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager.UnitTests;
using NuGet.Services.Staging.PackageService;
using Xunit;
using Stage = NuGet.Services.Staging.Database.Models.Stage;

namespace NuGet.Services.Staging.UnitTests.BackgroundWorkersUnitTests
{
    public class CommitStatusServiceUnitTests
    {
        private readonly CommitStatusService _commitStatusService;
        private readonly StageContextMock _stageContextMock;

        public CommitStatusServiceUnitTests()
        {
            _stageContextMock = new StageContextMock();
            _commitStatusService = new CommitStatusService(_stageContextMock.Object);            
        }

        [Fact]
        public void WhenGetCommitIsCalledLatestCommitReturned()
        {
            // Arrange
            string stageId = Guid.NewGuid().ToString();
            DateTime latestCommitTime = DateTime.UtcNow;

            _stageContextMock.Object.Stages.Add(new Database.Models.Stage
            {
                Id = stageId,
                Commits = new List<StageCommit>
                {
                    new StageCommit { RequestTime = latestCommitTime - TimeSpan.FromMinutes(10) },
                    new StageCommit { RequestTime = latestCommitTime }
                }
            });

            // Act
            var commit = _commitStatusService.GetCommit(stageId);

            // Assert
            commit.RequestTime.ShouldBeEquivalentTo(latestCommitTime);
        }

        [Theory]
        [InlineData(PushProgressStatus.Pending, CommitStatus.Pending)]
        [InlineData(PushProgressStatus.Completed, CommitStatus.Completed)]
        [InlineData(PushProgressStatus.Failed, CommitStatus.Failed)]
        [InlineData(PushProgressStatus.InProgress, CommitStatus.InProgress)]
        public async Task WhenUpdateProgressIsCalledCommitIsUpdated(PushProgressStatus progressStatus, CommitStatus expectedCommitStatus)
        {
            // Arrange
            var commit = new StageCommit
            {
                Status = CommitStatus.Pending,
                LastProgressUpdate = DateTime.MinValue
            };

            var report = new BatchPushProgressReport
            {
                Status = progressStatus
            };

            // Act
            await _commitStatusService.UpdateProgress(commit, report);

            // Assert
            commit.Status.ShouldBeEquivalentTo(expectedCommitStatus);
            commit.Progress.Should().NotBeEmpty();
            commit.LastProgressUpdate.Should().NotBe(DateTime.MinValue);
        }
    }
}