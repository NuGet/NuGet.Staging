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
using Stage.Database.Models;
using Stage.Manager.Controllers;
using Stage.Packages;
using Xunit;

namespace Stage.Manager.UnitTests
{
    // When nuspec is invalid 500 is returned
    // client version
    // package already exists on stage
    // user can't write to regId
    // package exists in nuget org

    public class PackageControllerUnitTests
    {
        private StageContextMock _stageContextMock;
        private PackageController _packageController;
        private Mock<IPackageService> _packageServiceMock;

        public PackageControllerUnitTests()
        {
            // Arrange
            _packageServiceMock = new Mock<IPackageService>();
            _stageContextMock = new StageContextMock();

            var stageServiceMock = new Mock<StageService>(_stageContextMock.Object)
            {
                CallBase = true
            };

            stageServiceMock.Setup(x => x.GetStage(It.IsAny<string>()))
                .Returns((string id) => _stageContextMock.Object.Stages.FirstOrDefault(x => x.Id == id));

            _packageController = new PackageController(
                new Mock<ILogger<PackageController>>().Object,
                _stageContextMock.Object,
                _packageServiceMock.Object,
                stageServiceMock.Object);
        }

        [Fact]
        public async Task WhenPushIsCalledPackageIsSavedAnd201IsReturned()
        {
            // Arrange
            var actionContext = new ActionContext();
            TestPackage testPackage = new TestPackage("DefaultId", "1.0.0");
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockForm = new Mock<IFormCollection>();
            var formFileCollection = new Mock<IFormFileCollection>();
            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(x => x.OpenReadStream()).Returns(testPackage.Stream);
            formFileCollection.Setup(x => x[It.IsAny<int>()]).Returns(formFileMock.Object);
            mockForm.Setup(x => x.Files).Returns(formFileCollection.Object);
            mockRequest.Setup(x => x.Form).Returns(mockForm.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            actionContext.HttpContext = mockHttpContext.Object;
            _packageController.ActionContext = actionContext;

            var stage = AddMockStage();

            try
            {
                IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidPackage500IsReturned()
        {
            // Arrange
            var actionContext = new ActionContext();
            TestPackage testPackage = new TestPackage("DefaultId", "1.0.0");
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockForm = new Mock<IFormCollection>();
            var formFileCollection = new Mock<IFormFileCollection>();
            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(x => x.OpenReadStream()).Returns(testPackage.Stream);
            formFileCollection.Setup(x => x[It.IsAny<int>()]).Returns(formFileMock.Object);
            mockForm.Setup(x => x.Files).Returns(formFileCollection.Object);
            mockRequest.Setup(x => x.Form).Returns(mockForm.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            actionContext.HttpContext = mockHttpContext.Object;
            _packageController.ActionContext = actionContext;

            var stage = AddMockStage();

            try
            {
                IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Database.Models.Stage AddMockStage()
        {
            const int stageKey = 1;

            var member = new StageMember
            {
                Key = 1,
                MemberType = MemberType.Owner,
                StageKey = stageKey,
                UserKey = 1
            };

            var stage = new Database.Models.Stage
            {
                Key = stageKey,
                Id = Guid.NewGuid().ToString(),
                DisplayName = "DefaultStage",
                Members = new List<StageMember> { member },
                Packages = new List<StagedPackage>()
            };

            _stageContextMock.Object.Stages.Add(stage);
            _stageContextMock.Object.StageMembers.Add(member);

            return stage;
        }
    }
}
