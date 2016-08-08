// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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
            // Act
            string[] args = { "stage", "create", "abc", "-Source", "http://api.nuget.org/v3/index.json" };
            var result = CommandRunner.Run(NuGetExe, Directory.GetCurrentDirectory(), string.Join(" ", args), true);

            // Assert
            Assert.Equal(1, result.Item1);
            var error = result.Item3;
            Assert.Contains("No API Key was provided", error);
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

        private MockServer CreateV3Server(JObject indexJson)
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
