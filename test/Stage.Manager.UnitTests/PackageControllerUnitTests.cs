// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
using Stage.V3;
using Xunit;

namespace Stage.Manager.UnitTests
{
    public class PackageControllerUnitTests
    {
        private const string DefaultRegistrationId = "DefaultId";
        private const string DefaultVersion = "1.0.0";

        private StageContextMock _stageContextMock;
        private PackageController _packageController;
        private Mock<IPackageService> _packageServiceMock;

        public PackageControllerUnitTests()
        {
            // Arrange
            _packageServiceMock = new Mock<IPackageService>();
            _packageServiceMock.Setup(x => x.IsUserOwnerOfPackageAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            _stageContextMock = new StageContextMock();

            var stageServiceMock = new Mock<StageService>(_stageContextMock.Object)
            {
                CallBase = true
            };

            stageServiceMock.Setup(x => x.GetStage(It.IsAny<string>()))
                .Returns((string id) => _stageContextMock.Object.Stages.FirstOrDefault(x => x.Id == id));

            var v3FactoryMock = new Mock<IV3ServiceFactory>();
            v3FactoryMock.Setup(x => x.Create(It.IsAny<string>())).Returns(new Mock<IV3Service>().Object);

            _packageController = new PackageController(
                new Mock<ILogger<PackageController>>().Object,
                _stageContextMock.Object,
                _packageServiceMock.Object,
                stageServiceMock.Object,
                v3FactoryMock.Object);
        }

        [Fact]
        public async Task WhenPushIsCalledPackageIsSavedAnd201IsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            stage.Packages.Count().Should().Be(1);
            stage.Packages.First().Id.Should().Be(DefaultRegistrationId);
            stage.Packages.First().Version.Should().Be(DefaultVersion);
            stage.Packages.First().NormalizedVersion.Should().Be(DefaultVersion);

            actionResult.Should().BeOfType<HttpStatusCodeResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidPackage400IsReturned()
        {
            // Arrange
            byte[] data = new byte[100];
            ArrangeRequestFileFromStream(new MemoryStream(data));

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidNuspec400IsReturned()
        {
            // Arrange
            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithInvalidNuspec();
            ArrangeRequestFileFromStream(package.Stream);

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidMinClientVerison400IsReturned()
        {
            // Arrange
            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithMinClientVersion("9.9.9");
            ArrangeRequestFileFromStream(package.Stream);

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledAndPackageExistsOnStage409IsReturned()
        {
            // Arrange
            var stage = AddMockStage();

            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithDefaultData();
            ArrangeRequestFileFromStream(package.Stream);

            await _packageController.PushPackageToStage(stage.Id);

            var samePackage = new TestPackage(DefaultRegistrationId, DefaultVersion).WithDefaultData();
            ArrangeRequestFileFromStream(samePackage.Stream);

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task WhenPushIsCalledAndUserIsNotOwner403IsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            _packageServiceMock.Setup(x => x.IsUserOwnerOfPackageAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(false));

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task WhenPushIsCalledAndPackageExistsWarningMessageIsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            _packageServiceMock.Setup(x => x.DoesPackageExistsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage.Id);

            // Assert
            stage.Packages.Count().Should().Be(1);

            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(201);
            ((string) objectResult.Value).Should().Contain(DefaultRegistrationId);
        }


        private void ArrangeRequestWithPackage(string id = DefaultRegistrationId, string version = DefaultVersion)
        {
            var testPackage = new TestPackage(id, version).WithDefaultData();
            ArrangeRequestFileFromStream(testPackage.Stream);
        }

        private void ArrangeRequestFileFromStream(Stream stream)
        {
            var actionContext = new ActionContext();
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockForm = new Mock<IFormCollection>();
            var formFileCollection = new Mock<IFormFileCollection>();
            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(x => x.OpenReadStream()).Returns(stream);
            formFileCollection.Setup(x => x[It.IsAny<int>()]).Returns(formFileMock.Object);
            mockForm.Setup(x => x.Files).Returns(formFileCollection.Object);
            mockRequest.Setup(x => x.Form).Returns(mockForm.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            actionContext.HttpContext = mockHttpContext.Object;
            _packageController.ActionContext = actionContext;
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
