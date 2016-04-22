// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;
using NuGet.Versioning;
using Xunit;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public class StageCommitWorkerUnitTests
    {
        public delegate List<PackagePushData> CreateInput(StageCommitWorkerUnitTests runContext);
        public delegate void VerifyPushOrder(List<PackagePushData> pushData, List<PackagePushData> pushOrder);

        private readonly StageCommitWorker _stageCommitWorker;
        private readonly Mock<ICommitStatusService> _commitStatusServiceMock;
        private readonly Mock<IPackageMetadataService> _packageMetadataServiceMock;
        private readonly Mock<IPackagePushService> _packagePushService;
        private readonly List<BatchPushProgressReport> _progressReports;
        private readonly List<PackagePushData> _pushList;

        public static IEnumerable<object[]> _pushOrderTestList
        {
            get
            {
                return new[]
                {
                    new object[] // Test dependency 3 <- 2 <- 1
                    {
                        new CreateInput(o =>
                        {
                            var packages = BatchCreatePackages(3);

                            // Setup dependencies: 3 <- 2, 2 <- 1
                            o.AddDependency(packages[0], packages[1]);
                            o.AddDependency(packages[1], packages[2]);

                            return packages;
                        }),
                        new VerifyPushOrder((pushData, pushOrder) =>
                        {
                            pushOrder.Count.Should().Be(3, "All packages were pushed");
                            pushOrder[0].Should().Be(pushData[2]);
                            pushOrder[1].Should().Be(pushData[1]);
                            pushOrder[2].Should().Be(pushData[0]);
                        })
                    },
                    new object[]  // Test dependency 4 <- 3, 4 <- 2 <- 1
                    {
                        new CreateInput(o =>
                        {
                            var packages = BatchCreatePackages(4);

                            // Setup dependencies: 4 <- 3, 4 <- 2 <- 1
                            o.AddDependency(packages[0], packages[1]);
                            o.AddDependency(packages[1], packages[3]);
                            o.AddDependency(packages[2], packages[3]);

                            return packages;
                        }),
                        new VerifyPushOrder((pushData, pushOrder) =>
                        {
                            pushOrder.Count.Should().Be(4, "All packages were pushed");
                            pushOrder[0].Should().Be(pushData[3], "package4 should be first");

                            int package2Order = pushOrder.FindIndex(x => x == pushData[1]);
                            int package1Order = pushOrder.FindIndex(x => x == pushData[0]);
                            package2Order.Should().BeLessThan(package1Order, "package2 should be pushed before package1");
                        })
                    },
                    new object[] // Test dependency json.net <- 2 <- 1 (dependency on server package)
                    {
                        new CreateInput(o =>
                        {
                            var packages = BatchCreatePackages(2);
                            var serverPackage = CreatePackage("json.net");

                            // Setup dependencies: json.net <- 2 <- 1
                            o.AddDependency(packages[0], packages[1]);
                            o.AddDependency(packages[1], serverPackage);

                            return packages;
                        }),
                        new VerifyPushOrder((pushData, pushOrder) =>
                        {
                            pushOrder.Count.Should().Be(2, "All packages were pushed");
                            pushOrder[0].Should().Be(pushData[1]);
                            pushOrder[1].Should().Be(pushData[0]);
                        })
                    },
                    new object[] // Test packages with same id, different versions
                    {
                        new CreateInput(o => new List<PackagePushData>()
                        {
                            CreatePackage("package", "1.0.0"),
                            CreatePackage("package", "2.0.0")
                        }),
                        new VerifyPushOrder((pushData, pushOrder) =>
                        {
                            pushOrder.Count.Should().Be(2, "All packages were pushed");
                        })
                    }
                };
            }
        }

        public StageCommitWorkerUnitTests()
        {
            _commitStatusServiceMock = new Mock<ICommitStatusService>();
            _packageMetadataServiceMock = new Mock<IPackageMetadataService>();
            _packagePushService = new Mock<IPackagePushService>();
            _progressReports = new List<BatchPushProgressReport>();
            _pushList = new List<PackagePushData>();

            _commitStatusServiceMock.Setup(x => x.UpdateProgress(It.IsAny<StageCommit>(), It.IsAny<BatchPushProgressReport>()))
                                        .Returns(Task.FromResult(0))
                                        .Callback<StageCommit, BatchPushProgressReport>((s, pr) =>
                                        {
                                            // Create a copy and store
                                            var json = JsonConvert.SerializeObject(pr);
                                            var copy = JsonConvert.DeserializeObject<BatchPushProgressReport>(json);
                                            _progressReports.Add(copy);
                                        });

            _packagePushService.Setup(x => x.PushPackage(It.IsAny<PackagePushData>()))
                               .Returns(Task.FromResult(new PackagePushResult
                               {
                                   Status = PackagePushStatus.Success
                               }))
                               .Callback<PackagePushData>(x => _pushList.Add(x));

            _stageCommitWorker = new StageCommitWorker(
                new Mock<IMessageListener<PackageBatchPushData>>().Object,
                _commitStatusServiceMock.Object,
                _packageMetadataServiceMock.Object,
                _packagePushService.Object,
                new Mock<ILogger<StageCommitWorker>>().Object);
        }

        [Fact]
        public async Task WhenCommitNotFoundSuccess()
        {
            // Act
            await _stageCommitWorker.HandleBatchPushRequest(new PackageBatchPushData(), isLastDelivery: false);

            // Assert no exception was thrown
        }

        [Theory]
        [InlineData(CommitStatus.Completed)]
        [InlineData(CommitStatus.Failed)]
        public async Task WhenCommitDoneOrFailedSuccess(CommitStatus commitStatus)
        {
            // Assert
            _commitStatusServiceMock.Setup(x => x.GetCommit(It.IsAny<string>())).Returns(new StageCommit
            {
                Status = commitStatus
            });

            // Act
            await _stageCommitWorker.HandleBatchPushRequest(new PackageBatchPushData(), isLastDelivery: false);

            // Assert no exception was thrown
        }

        [Theory]
        [MemberData("_pushOrderTestList")]
        public async Task VerifyHappyFlow(CreateInput initializePackages, VerifyPushOrder verifyPushOrder)
        {
            // Arrange
            var packages = initializePackages(this);
             var batchPushData = new PackageBatchPushData
            {
                StageId = Guid.NewGuid().ToString(),
                PackagePushDataList = packages
             };

            var stageCommit = new StageCommit { Status = CommitStatus.Pending };
            _commitStatusServiceMock.Setup(x => x.GetCommit(batchPushData.StageId)).Returns(stageCommit);

            // Act
            await _stageCommitWorker.HandleBatchPushRequest(batchPushData, isLastDelivery: false);

            // Assert
            verifyPushOrder(packages, _pushList);

            _progressReports.Count.Should().Be(_pushList.Count*2);
            int currentProgressReport = 0;

            for (int i = 0; i < _pushList.Count; i++)
            {
                var inProgress = _pushList[i];
                var completed = i == 0 ? new List<PackagePushData>() : _pushList.GetRange(0, i);
                var pending = i == _pushList.Count-1 ? new List<PackagePushData>() : _pushList.GetRange(i+1, _pushList.Count - i - 1);

                VerifyProgressReport(_progressReports[currentProgressReport++], completed, inProgress, pending, PushProgressStatus.InProgress);
                VerifyProgressReport(_progressReports[currentProgressReport++], completed, inProgress, pending, PushProgressStatus.Completed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenCommitIsRetriedSucceed(bool pushCompleted)
        {
            // Arrange
            var packages = BatchCreatePackages(3);

            var batchPushData = new PackageBatchPushData
            {
                StageId = Guid.NewGuid().ToString(),
                PackagePushDataList = packages
            };
           
            var progressReport = new BatchPushProgressReport
            {
                Status = PushProgressStatus.InProgress,
                PackagePushProgressReports = new List<PackagePushProgressReport>
                {
                    new PackagePushProgressReport
                    {
                        Id = packages[0].Id,
                        Version = packages[0].Version,
                        Status = PushProgressStatus.Completed,
                    },
                    new PackagePushProgressReport
                    {
                        Id = packages[1].Id,
                        Version = packages[1].Version,
                        Status = PushProgressStatus.InProgress,
                    },
                    new PackagePushProgressReport
                    {
                        Id = packages[2].Id,
                        Version = packages[2].Version,
                        Status = PushProgressStatus.Pending
                    }
                }
            };

            var stageCommit = new StageCommit
            {
                Status = CommitStatus.InProgress,
                Progress = JsonConvert.SerializeObject(progressReport)
            };

            _commitStatusServiceMock.Setup(x => x.GetCommit(batchPushData.StageId)).Returns(stageCommit);

            if (pushCompleted)
            {
                _packagePushService.Setup(x => x.PushPackage(packages[1]))
                    .Returns(Task.FromResult(new PackagePushResult { Status = PackagePushStatus.AlreadyExists }))
                    .Callback<PackagePushData>(x => _pushList.Add(x)); ;
            }

            // Act
            await _stageCommitWorker.HandleBatchPushRequest(batchPushData, isLastDelivery: false);

            // Assert
            _pushList.Count.Should().Be(2, "All unpushed were pushed");
            _progressReports.Count.Should().Be(_pushList.Count*2-1);

            VerifyProgressReport(_progressReports[0], new List<PackagePushData> { packages[0] }, packages[1], new List<PackagePushData> { packages[2] }, PushProgressStatus.Completed);
            VerifyProgressReport(_progressReports[1], new List<PackagePushData> { packages[0], packages[1] }, packages[2], new List<PackagePushData>(), PushProgressStatus.InProgress);
            VerifyProgressReport(_progressReports[2], new List<PackagePushData> { packages[0], packages[1] }, packages[2], new List<PackagePushData>(), PushProgressStatus.Completed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WhenExceptionIsThrownAndLastDelieveryProgressUpdated(bool isLastDelivery)
        {
            var package = CreatePackage("package");

            var batchPushData = new PackageBatchPushData
            {
                StageId = Guid.NewGuid().ToString(),
                PackagePushDataList = new List<PackagePushData>() { package }
            };

            var stageCommit = new StageCommit { Status = CommitStatus.Pending };
            _commitStatusServiceMock.Setup(x => x.GetCommit(batchPushData.StageId)).Returns(stageCommit);

            int count = 0;
            BatchPushProgressReport progressReport = null;

            // Throw for the first time, succeed the second time
            _commitStatusServiceMock.Setup(x => x.UpdateProgress(It.IsAny<StageCommit>(), It.IsAny<BatchPushProgressReport>()))
                                    .Returns(() =>
                                    {
                                        if (count == 0)
                                        {
                                            count++;
                                            throw new ArgumentException();
                                        }

                                        return Task.FromResult(0);
                                    })
                                    .Callback<StageCommit, BatchPushProgressReport>((s, pr) =>
                                    {
                                        progressReport = pr;
                                    });

            // Act & Assert
            Func<Task> act = async () => { await _stageCommitWorker.HandleBatchPushRequest(batchPushData, isLastDelivery); };
            act.ShouldThrow<ArgumentException>();

            if (isLastDelivery)
            {
                progressReport.Status.Should().Be(PushProgressStatus.Failed);
                progressReport.FailureDetails.Should().NotBeEmpty();
            }
        }

        private void AddDependency(PackagePushData from, PackagePushData to)
        {
            _packageMetadataServiceMock.Setup(x => x.GetPackageDependencies(from))
                .Returns(Task.FromResult((IEnumerable<PackageDependency>)new List<PackageDependency>()
                {
                    new PackageDependency(to.Id, new VersionRange(new NuGetVersion(to.Version)))
                }));
        }

        private static List<PackagePushData> BatchCreatePackages(int count)
        {
            return Enumerable.Range(1, count).Select(i => CreatePackage("package" + i)).ToList();
        } 

        private static PackagePushData CreatePackage(string id, string version = "1.0.0")
        {
            return new PackagePushData
            {
                Id = id,
                Version = version,
            };
        }

        private void VerifyProgressReport(BatchPushProgressReport progressReport,
         IEnumerable<PackagePushData> completedPackages, PackagePushData inProgressPackage,
         IEnumerable<PackagePushData> pendingPackages, PushProgressStatus expectedPushStatus)
        {
            foreach (var completedPackage in completedPackages)
            {
                progressReport.PackagePushProgressReports.First(x => x.Id == completedPackage.Id && x.Version == completedPackage.Version).Status
                    .Should().Be(PushProgressStatus.Completed, "Package {0} should be completed", completedPackage.Id);
            }

            foreach (var pendingPackage in pendingPackages)
            {
                progressReport.PackagePushProgressReports.First(x => x.Id == pendingPackage.Id && x.Version == pendingPackage.Version).Status
                    .Should().Be(PushProgressStatus.Pending, "Package {0} should be pending", pendingPackage.Id);
            }

            progressReport.PackagePushProgressReports.First(x => x.Id == inProgressPackage.Id && x.Version == inProgressPackage.Version).Status
                .Should().Be(expectedPushStatus, "Package {0} should be in-progress", inProgressPackage.Id);

            if (expectedPushStatus == PushProgressStatus.Completed && !pendingPackages.Any())
            {
                progressReport.Status.Should().Be(PushProgressStatus.Completed);
            }
            else
            {
                progressReport.Status.Should().Be(PushProgressStatus.InProgress);
            }
        }
    }
}