// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.V3Repository;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager;
using NuGet.Services.Staging.Manager.Controllers;
using NuGet.Services.Staging.PackageService;
using NuGet.Services.Test.Common;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest
{
    [Collection("Packages test collection")]
    public class PackageControllerUnitTests
    {
        private const string DefaultRegistrationId = "DefaultId";
        private const string DefaultVersion = "1.0.0";
        private const string BaseAddress = "http://nuget.org/";
        protected UserInformation DefaultUser = new UserInformation { UserKey = 2, UserName = "testUser" };

        private StageContextMock _stageContextMock;
        private PackageController _packageController;
        private Mock<IPackageService> _packageServiceMock;
        private TestStorageFactory _testStorageFactory;
        private Mock<HttpContext> _httpContextMock; 

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

            var options = new Mock<IOptions<V3ServiceOptions>>();


            options.Setup(x => x.Value).Returns(new V3ServiceOptions
            {
                CatalogFolderName = "catalog",
                FlatContainerFolderName = "flatcontainer",
                RegistrationFolderName = "registration",
            });

            _testStorageFactory = new TestStorageFactory((string s) => new MemoryStorage(new Uri(BaseAddress + s)));
            var v3Factory = new V3ServiceFactory(options.Object, _testStorageFactory, new Mock<ILogger<V3Service>>().Object);

            _packageController = new PackageController(
                new Mock<ILogger<PackageController>>().Object,
                _packageServiceMock.Object,
                stageServiceMock.Object,
                v3Factory);

            _httpContextMock = _packageController.WithMockHttpContext().WithUser(DefaultUser);
        }

        [Fact]
        public async Task WhenPushIsCalledPackageIsSavedAnd201IsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            stage.Packages.Count.Should().Be(1);

            var package = stage.Packages.First();

            package.Id.Should().Be(DefaultRegistrationId);
            package.Version.Should().Be(DefaultVersion);
            package.NormalizedVersion.Should().Be(DefaultVersion);

            package.PackageMetadata.Id.Should().Be(DefaultRegistrationId);
            package.PackageMetadata.Version.Should().Be(DefaultVersion);
            package.PackageMetadata.Owners.Should().Be(DefaultUser.UserName);
            package.PackageMetadata.Authors.Should().Be(TestPackage.DefaultAuthors);
            package.PackageMetadata.Description.Should().Be(TestPackage.DefaultDescription);
            package.PackageMetadata.IconUrl.Should().Be(TestPackage.DefaultIconUrl);
            package.PackageMetadata.LicenseUrl.Should().Be(TestPackage.DefaultLicenseUrl);
            package.PackageMetadata.ProjectUrl.Should().Be(TestPackage.DefaultProjectUrl);
            package.PackageMetadata.Summary.Should().Be(TestPackage.DefaultSummary);
            package.PackageMetadata.Tags.Should().Be(TestPackage.DefaultTags);
            package.PackageMetadata.Title.Should().Be(TestPackage.DefaultTitle);

            actionResult.Should().BeOfType<StatusCodeResult>();

            _testStorageFactory.CreatedStorages.Any().Should().BeTrue();
            ((MemoryStorage) _testStorageFactory.CreatedStorages.Values.First()).Content.Count()
                .Should()
                .BeGreaterThan(0, "Files should exist");
            _testStorageFactory.CreatedStorages.Keys.Any(x => !x.StartsWith($"{stage.Id}"))
                .Should()
                .BeFalse("No files should be outside of stage folder");
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidPackage400IsReturned()
        {
            // Arrange
            byte[] data = new byte[100];
            _httpContextMock.WithFile(new MemoryStream(data));

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidNuspec400IsReturned()
        {
            // Arrange
            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithInvalidNuspec();
            _httpContextMock.WithFile(package.Stream);

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledWithInvalidMinClientVerison400IsReturned()
        {
            // Arrange
            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithMinClientVersion("9.9.9");
            _httpContextMock.WithFile(package.Stream);

            var stage = AddMockStage();

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenPushIsCalledAndPackageExistsOnStage409IsReturned()
        {
            // Arrange
            var stage = AddMockStage();

            var package = new TestPackage(DefaultRegistrationId, DefaultVersion).WithDefaultData();
            _httpContextMock.WithFile(package.Stream);

            await _packageController.PushPackageToStage(stage);

            var samePackage = new TestPackage(DefaultRegistrationId, DefaultVersion).WithDefaultData();
            _httpContextMock.WithFile(samePackage.Stream);

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task WhenPushIsCalledAndUserIsNotOwnerOfPackage409IsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            _packageServiceMock.Setup(x => x.IsUserOwnerOfPackageAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(false));

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task WhenPushIsCalledAndPackageExistsWarningMessageIsReturned()
        {
            // Arrange
            ArrangeRequestWithPackage();
            var stage = AddMockStage();

            _packageServiceMock.Setup(x => x.DoesPackageExistsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            stage.Packages.Count.Should().Be(1);

            actionResult.Should().BeOfType<ObjectResult>();
            var objectResult = actionResult as ObjectResult;
            objectResult.StatusCode.Should().Be(201);
            ((string) objectResult.Value).Should().Contain(DefaultRegistrationId);
        }

        [Fact]
        public void VerifyPushRequiresAuthentication()
        {
            // Assert
            AuthorizationTest.IsAuthorized(_packageController, "PushPackageToStage", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void WhenPushIsCalledAndUserIsNotOwnerOfStage401IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<EnsureStageExistsFilter>(_packageController, "PushPackageToStage", methodTypes: null).Should().BeTrue();
        }

        [Theory]
        [InlineData(StageStatus.Committing)]
        [InlineData(StageStatus.Committed)]
        public async Task WhenPushIsCalledAndStageIsCommiting400IsReturned(StageStatus status)
        {
            // Arrange
            var stage = AddMockStage();
            stage.Status = status;

            // Act
            IActionResult actionResult = await _packageController.PushPackageToStage(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        private void ArrangeRequestWithPackage(string id = DefaultRegistrationId, string version = DefaultVersion)
        {
            var testPackage = new TestPackage(id, version).WithDefaultData();
            _httpContextMock.WithFile(testPackage.Stream);
        }

        private Stage AddMockStage()
        {
            const int stageKey = 1;

            var member = new StageMembership
            {
                Key = 1,
                MembershipType = MembershipType.Owner,
                StageKey = stageKey,
                UserKey = DefaultUser.UserKey
            };

            var stage = new Stage
            {
                Key = stageKey,
                Id = Guid.NewGuid().ToString(),
                DisplayName = "DefaultStage",
                Memberships = new List<StageMembership> { member },
                Packages = new List<StagedPackage>()
            };

            _stageContextMock.Object.Stages.Add(stage);
            _stageContextMock.Object.StageMemberships.Add(member);

            return stage;
        }
    }
}
