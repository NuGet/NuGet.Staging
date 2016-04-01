// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.v3;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.Database.Models;
using Stage.Manager.Controllers;
using Stage.Manager.Search;
using Xunit;

namespace Stage.Manager.UnitTests
{
    /// <summary>
    /// Helpful links:
    /// https://blogs.msdn.microsoft.com/webdev/2015/08/06/unit-testing-with-dnx-asp-net-5-projects/
    /// http://www.jerriepelser.com/blog/unit-testing-controllers-aspnet5
    /// </summary>
    public class StageControllerUnitTests
    {
        const string _displayName = "display name";

        private StageController _stageController;
        private StageContextMock _stageContextMock;

        public StageControllerUnitTests()
        {
            // Arrange
            _stageContextMock = new StageContextMock();

            var stageServiceMock = new Mock<StageService>(_stageContextMock.Object)
            {
                CallBase = true
            };

            stageServiceMock.Setup(x => x.GetStage(It.IsAny<string>()))
                .Returns((string id) => _stageContextMock.Object.Stages.FirstOrDefault(x => x.Id == id));

            _stageController = new StageController(
                new Mock<ILogger<StageController>>().Object,
                _stageContextMock.Object,
                stageServiceMock.Object,
                new Mock<StorageFactory>().Object, 
                new Mock<ISearchService>().Object);

            MockRequest();
        }

        [Fact]
        public async Task WhenCreateIsCalledNewStageIsAdded()
        {
            // Act 
            IActionResult actionResult =  await _stageController.Create(_displayName);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();
            _stageContextMock.Object.Stages.Count().Should().Be(1);

            Database.Models.Stage stage = _stageContextMock.Object.Stages.First();

            stage.DisplayName.Should().Be(_displayName);
            stage.Status.Should().Be(StageStatus.Active);
            stage.Members.Count.Should().Be(1);
            stage.Members.First().MemberType.Should().Be(MemberType.Owner);
        }

        [Fact]
        public async Task WhenCreateCalledWithInvalidDisplayName400IsReturned()
        {
            // Act 
            IActionResult actionResult = await _stageController.Create("");

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenCreateCalledWithLongDisplayName400IsReturned()
        {
            // Act 
            IActionResult actionResult = await _stageController.Create("abcdefghijklmnoprstuvwxyzabcdefghijklmnoprstuvwxyz");

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenListUserStagesCalledStagesAreReturned()
        {
            // Arrange
            await AddMockStage("first");
            await AddMockStage("second");

            // Act
            IActionResult actionResult = _stageController.ListUserStages();

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
            int count = (int)result.GetType().GetProperty("Count").GetValue(result);

            count.Should().Be(2);
        }

        [Fact]
        public async Task WhenGetDetailsIsCalledDetailsAreReturned()
        {
            // Arrange
            await AddMockStage("first");
            string secondStageId = await AddMockStage(_displayName);
            AddMockPackage(secondStageId, "package");

            // Act
            IActionResult actionResult = _stageController.GetDetails(secondStageId);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            var result = (actionResult as HttpOkObjectResult).Value;
            result.Should().BeOfType<StageController.ExternalStageDetailed>();

            var stageDetails = result as StageController.ExternalStageDetailed;

            stageDetails.Metadata.DisplayName.Should().Be(_displayName);
            stageDetails.Packages.Count.Should().Be(1);
        }

        [Fact]
        public void WhenGetDetailsIsCalledWithNonExistingStageId404IsReturned()
        {
            // Act
            IActionResult actionResult = _stageController.GetDetails(Guid.NewGuid().ToString());

            // Assert
            actionResult.Should().BeOfType<HttpNotFoundResult>();
        }

        [Fact]
        public async Task WhenDropIsCalledStageIsDropped()
        {
            const string stageName1 = "first";
            const string stageName2 = "second";

            // Arrange
            string stageId1 = await AddMockStage(stageName1);
            string stageId2 = await AddMockStage(stageName2);

            // Act
            IActionResult actionResult = await _stageController.Drop(stageId1);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
            string displayName = (string)result.GetType().GetProperty("DisplayName").GetValue(result);

            displayName.Should().Be(stageName1);

            _stageContextMock.Object.Stages.Count().Should().Be(1);
            _stageContextMock.Object.Stages.First().Id.Should().Be(stageId2);
        }

        [Fact]
        public async Task WhenDropIsCalledWithNonExistingStageId404IsReturned()
        {
            // Act
            IActionResult actionResult = await _stageController.Drop(Guid.NewGuid().ToString());

            // Assert
            actionResult.Should().BeOfType<HttpNotFoundResult>();
        }

        [Fact]
        public void WhenIndexIsCalledJsonIsReturned()
        {
            // Act
            IActionResult actionResult = _stageController.Index(Guid.NewGuid().ToString());

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult) actionResult;
            ((JObject) jsonResult.Value).ToString().Should().NotBeEmpty();
            var jsonObj = (JObject) jsonResult.Value;
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[0]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[1]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchAutocompleteService).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[0]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[1]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.PackageBaseAddress).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.PackagePublish).Should().NotBeEmpty();
        }

        private void MockRequest()
        {
            var actionContext = new ActionContext();
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();

            mockRequest.Setup(x => x.Scheme).Returns("http");
            mockRequest.Setup(x => x.Host).Returns(new HostString("stage.nuget.org"));
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            actionContext.HttpContext = mockHttpContext.Object;
            _stageController.ActionContext = actionContext;
        }

        private async Task<string> AddMockStage(string displayName)
        {
            IActionResult actionResult = await _stageController.Create(displayName);

            var stage = _stageContextMock.Object.Stages.Last();
            _stageContextMock.Object.StageMembers.AddRange(stage.Members);

            foreach (var member in stage.Members)
            {
                member.Stage = stage;
            }
            stage.Packages = new List<StagedPackage>();
            object result = (actionResult as HttpOkObjectResult).Value;
            return (string)result.GetType().GetProperty("Id").GetValue(result);
        }

        private void AddMockPackage(string stageId, string packageId)
        {
            var stage = _stageContextMock.Object.Stages.First(x => x.Id == stageId);
            stage.Packages.Add(new StagedPackage
            {
                Id = packageId,
                Version = "1.0.0"
            });
        }
    }
}
