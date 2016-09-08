// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Client.Staging;
using NuGet.CommandLine.StagingExtensions;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.CommandLine.Staging.UnitTests
{
    public class StagingCommandUnitTests
    {
        private readonly string NuGetExe;
        public StagingCommandUnitTests()
        {
            Util.ClearWebCache();
            NuGetExe = Util.GetNuGetExePath();
        }

        [Fact]
        public void StageCommand_WhenSourceNotProvidedErrorIsShown()
        {
            // Act
            string[] args = { "stage", "create", "abc" };
            var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

            // Assert
            Assert.Equal(1, result.Item1);
            var error = result.Item3;
            Assert.Contains(NuGetResources.Error_MissingSourceParameter, error);
        }

        [Fact]
        public void StageCommand_WhenApiKeyNotProvidedErrorIsShown()
        {
            // Arrange
            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "create", "abc", "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains("No API Key was provided", error);
                }
            }
        }

        [Fact]
        public void StageCommand_WhenBadParametersProvidedHelpIsShown()
        {
            // Act
            string[] args = { "stage", "create", "abc", "drop", "cde" };
            var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

            // Assert
            Assert.Contains(StagingResources.StageCommandUsageSummary, result.Item2);
        }

        [Fact]
        public void StageCommand_WhenSourceDoesNotSupportStagingErrorIsShown()
        {
            // Arrange
            using (var serverV3 = CreateV3Server(Util.CreateIndexJson()))
            {
                serverV3.Start();

                // Act
                string[] args = { "stage", "create", "abc", "-Source", serverV3.Uri + "index.json", "-ApiKey", "123" };
                var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                serverV3.Stop();

                // Assert
                Assert.Equal(1, result.Item1);
                var error = result.Item3;
                Assert.Contains(StagingResources.StagingNotSupported, error);
            }
        }

        [Fact]
        public void CreateCommand_HappyFlow()
        {
            // Arrange
            const string feedUri = "http://api.nuget.org/stage/123/index.json";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Post.Add("/api/stage", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var createResult = new StageListView();
                            createResult.Feed = feedUri;
                            var json = JsonConvert.SerializeObject(createResult);

                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = {"stage", "create", "abc", "-Source", serverV3.Uri + "index.json", "-ApiKey", "123"};
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;
                    Assert.Contains(feedUri, output);
                }
            }
        }

        [Fact]
        public void CreateCommand_WhenStagingServiceReturnsAnErrorItIsShown()
        {
            // Arrange
            const string serverMessage = "Failure message";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Post.Add("/api/stage", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            response.StatusCode = 400;
                            MockServer.SetResponseContent(response, serverMessage);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "create", "abc", "-Source", serverV3.Uri + "index.json", "-ApiKey", "123" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains(serverMessage, error);
                }
            }
        }

        [Fact]
        public void DropCommand_HappyFlow()
        {
            // Arrange
            const string stageId = "4b139cb7-c4d4-4541-8c05-0f41ba5ab945";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Delete.Add($"/api/stage/{stageId}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var createResult = new StageView();
                            createResult.Id = stageId;
                            var json = JsonConvert.SerializeObject(createResult);

                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "drop", stageId, "-Source", serverV3.Uri + "index.json", "-ApiKey", "123", "-NonInteractive" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;
                    Assert.Contains(stageId, output);
                }
            }
        }

        [Fact]
        public void DropCommand_WhenStagingServiceReturnsAnErrorItIsShown()
        {
            // Arrange
            const string serverMessage = "Failure message";
            const string stageId = "4b139cb7-c4d4-4541-8c05-0f41ba5ab945";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Delete.Add($"/api/stage/{stageId}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            response.StatusCode = 400;
                            MockServer.SetResponseContent(response, serverMessage);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "drop", stageId, "-Source", serverV3.Uri + "index.json", "-ApiKey", "123", "-NonInteractive" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains(serverMessage, error);
                }
            }
        }

        [Fact]
        public void ListCommand_WhenNoStagesExistMessageIsPrinted()
        {
            // Arrange
            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add("/api/stage", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var json = JsonConvert.SerializeObject(new List<StageListView>());
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "list", "-Source", serverV3.Uri + "index.json", "-ApiKey", "123" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;
                    Assert.Contains(StagingResources.StageListNoStagesFound, output);
                }
            }
        }

        [Fact]
        public void ListCommand_WhenStagesExistTheyArePrinted()
        {
            // Arrange
            var stages = Enumerable.Range(1, 2).Select(i =>
                new StageListView
                {
                    DisplayName = "stage" + i,
                    CreationDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow + TimeSpan.FromDays(i),
                    Feed = "feed" + i,
                    Status = "status" + i,
                }).ToList();

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add("/api/stage", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var json = JsonConvert.SerializeObject(stages);
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "list", "-Source", serverV3.Uri + "index.json", "-ApiKey", "123" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;

                    foreach (var stage in stages)
                    {
                        Assert.Contains(stage.DisplayName, output);
                        Assert.Contains(stage.CreationDate.ToString(), output);
                        Assert.Contains(stage.ExpirationDate.ToString(), output);
                        Assert.Contains(stage.Status, output);
                    }
                }
            }
        }

        [Fact]
        public void GetCommand_WhenStagingServiceReturnsAnErrorItIsShown()
        {
            // Arrange
            const string stageId = "1323";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add($"/api/stage/{stageId}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            response.StatusCode = 404;
                            MockServer.SetResponseContent(response, string.Empty);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "get", stageId, "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains("NotFound", error);
                }
            }
        }

        [Fact]
        public void GetCommand_HappyFlow()
        {
            // Arrange
            var stageDetails = new StageDetailedView
            {
                Id = "1323",
                DisplayName = "name",
                CreationDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow + TimeSpan.FromDays(1),
                Feed = "http://abc",
                Status = "Active",
                PackagesCount = 2,
                Packages = new List<PackageView>
                {
                    new PackageView {Id = "first", Version = "1.0.0"},
                    new PackageView {Id = "second", Version = "2.0.0"}
                },
                Memberships = new List<MembershipView>
                {
                    new MembershipView {MembershipType = "Owner", Name = "aabb"},
                    new MembershipView {MembershipType = "Contributor", Name = "ccvv"}
                }
            };

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add($"/api/stage/{stageDetails.Id}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var json = JsonConvert.SerializeObject(stageDetails);
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "get", stageDetails.Id, "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;
                    
                    Assert.Contains(stageDetails.Id, output);
                    Assert.Contains(stageDetails.DisplayName, output);
                    Assert.Contains(stageDetails.CreationDate.ToString(), output);
                    Assert.Contains(stageDetails.ExpirationDate.ToString(), output);
                    Assert.Contains(stageDetails.Feed, output);
                    Assert.Contains(stageDetails.Status, output);

                    foreach (var membership in stageDetails.Memberships)
                    {
                        Assert.Contains(membership.MembershipType, output);
                        Assert.Contains(membership.Name, output);
                    }

                    foreach (var package in stageDetails.Packages)
                    {
                        Assert.Contains(package.Id, output);
                        Assert.Contains(package.Version, output);
                    }
                }
            }
        }

        [Fact]
        public void GetProgress_HappyFlow()
        {
            // Arrange
            const string stageId = "1323";

            var progressList = GenerateHappyFlowCommitProgress();

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    int counter = 0;

                    stagingServer.Get.Add($"/api/stage/{stageId}/commit", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var val = progressList[counter++];
                            var json = JsonConvert.SerializeObject(val);
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "progress", stageId, "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;

                    VerifyHappyFlowCommitProgress(output, progressList);
                }
            }
        }

        [Fact]
        public void GetProgress_WhenStagingServiceReturnsAnErrorItIsShown()
        {
            // Arrange
            const string stageId = "1323";
            const string errorMessage = "Commit doesn't exist";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add($"/api/stage/{stageId}/commit", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            response.StatusCode = 400;
                            MockServer.SetResponseContent(response, errorMessage);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "progress", stageId, "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains(errorMessage, error);
                }
            }
        }

        [Fact]
        public void GetProgress_WhenGetCommitProgressFailsErrorIsShown()
        {
            // Arrange
            const string stageId = "1323";

            var progress = new StageCommitProgressView
            {
                CommitStatus = StageCommand.CommitFailed,
                ErrorMessage = "Error message",
                PackageProgressList = new List<PackageCommitProgressView>
                {
                    new PackageCommitProgressView { Id = "abc", Version = "1.0.0", Progress = "Pending" }
                }
            };

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Get.Add($"/api/stage/{stageId}/commit", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var json = JsonConvert.SerializeObject(progress);
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "progress", stageId, "-Source", serverV3.Uri + "index.json" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;
                    var error = result.Item3;

                    Assert.Contains(StagingResources.StageCommitFailed, error);
                    Assert.Contains(progress.ErrorMessage, error);
                    Assert.Contains(progress.PackageProgressList[0].Id, output);
                    Assert.Contains(progress.PackageProgressList[0].Version, output);
                    Assert.Contains(progress.PackageProgressList[0].Progress, output);
                }
            }
        }

        [Fact]
        public void StageCommit_HappyFlow()
        {
            // Arrange
            const string stageId = "1323";

            var progressList = GenerateHappyFlowCommitProgress();

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Post.Add($"/api/stage/{stageId}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            MockServer.SetResponseContent(response, string.Empty);
                        }));

                    int counter = 0;

                    stagingServer.Get.Add($"/api/stage/{stageId}/commit", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            var val = progressList[counter++];
                            var json = JsonConvert.SerializeObject(val);
                            MockServer.SetResponseContent(response, json);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "commit", stageId, "-Source", serverV3.Uri + "index.json", "-ApiKey", "123", "-NonInteractive" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(0, result.Item1);
                    var output = result.Item2;

                    VerifyHappyFlowCommitProgress(output, progressList);
                }
            }
        }

        [Fact]
        public void StageCommit_WhenStagingServiceReturnsAnErrorItIsShown()
        {
            // Arrange
            const string stageId = "1323";
            const string errorMessage = "Error Message";

            var indexJson = Util.CreateIndexJson();

            using (var serverV3 = CreateV3Server(indexJson))
            {
                using (var stagingServer = new MockServer())
                {
                    Util.AddStagingResource(indexJson, stagingServer);

                    stagingServer.Post.Add($"/api/stage/{stageId}", r =>
                        new Action<HttpListenerResponse>(response =>
                        {
                            response.StatusCode = 400;
                            MockServer.SetResponseContent(response, errorMessage);
                        }));

                    serverV3.Start();
                    stagingServer.Start();

                    // Act
                    string[] args = { "stage", "commit", stageId, "-Source", serverV3.Uri + "index.json", "-ApiKey", "123", "-NonInteractive" };
                    var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

                    serverV3.Stop();
                    stagingServer.Stop();

                    // Assert
                    Assert.Equal(1, result.Item1);
                    var error = result.Item3;
                    Assert.Contains(errorMessage, error);
                }
            }
        }

        private static void VerifyHappyFlowCommitProgress(string output, List<StageCommitProgressView> progressList)
        {
            // Check pending status printout
            int pendingIdx = output.IndexOf(StagingResources.StageCommitPending);
            Assert.True(pendingIdx > 0);

            int separatorIdx = output.IndexOf("...");

            // Check in progress status printout
            int inprogressIdx = output.IndexOf(StagingResources.StageCommitInProgress, startIndex: separatorIdx);
            Assert.True(inprogressIdx > 0);

            foreach (var packageProgress in progressList[1].PackageProgressList)
            {
                Assert.True(output.IndexOf(packageProgress.Id, startIndex: inprogressIdx) > 0);
                Assert.True(output.IndexOf(packageProgress.Version, startIndex: inprogressIdx) > 0);
                Assert.True(
                    output.IndexOf(StageCommand.GetCommitStatusString(packageProgress.Progress), startIndex: inprogressIdx) > 0);
            }

            separatorIdx = output.IndexOf("...", startIndex: inprogressIdx);

            // Check completed status printout
            int completedIdx = output.IndexOf(StagingResources.StageCommitCompleted, startIndex: separatorIdx);
            Assert.True(completedIdx > 0);
        }

        private static List<StageCommitProgressView> GenerateHappyFlowCommitProgress()
        {
            var progressList = new List<StageCommitProgressView>
            {
                new StageCommitProgressView
                {
                    CommitStatus = StageCommand.CommitPending
                },
                new StageCommitProgressView
                {
                    CommitStatus = StageCommand.CommitInProgress,
                    PackageProgressList = new List<PackageCommitProgressView>
                    {
                        new PackageCommitProgressView
                        {
                            Id = "abc",
                            Version = "1.0.0",
                            Progress = StageCommand.CommitCompleted
                        },
                        new PackageCommitProgressView
                        {
                            Id = "abc1",
                            Version = "2.0.0",
                            Progress = StageCommand.CommitInProgress
                        },
                        new PackageCommitProgressView
                        {
                            Id = "abc2",
                            Version = "3.0.0",
                            Progress = StageCommand.CommitPending
                        }
                    }
                },
                new StageCommitProgressView
                {
                    CommitStatus = StageCommand.CommitCompleted
                }
            };

            return progressList;
        }

        private static MockServer CreateV3Server(JObject indexJson)
        {
            var serverV3 = new MockServer();

            serverV3.Get.Add("/", r =>
            {
                var path = r.Url.AbsolutePath;

                if (path == "/index.json")
                {
                    return new Action<HttpListenerResponse>(response =>
                    {
                        response.StatusCode = 200;
                        response.ContentType = "text/javascript";
                        MockServer.SetResponseContent(response, indexJson.ToString());
                    });
                }

                throw new Exception("This test needs to be updated to support: " + path);
            });

            return serverV3;
        }
    }
}
