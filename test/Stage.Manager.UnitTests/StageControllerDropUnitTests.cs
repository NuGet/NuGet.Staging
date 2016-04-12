// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Mvc;
using Xunit;

namespace Stage.Manager.UnitTests
{
    public class StageControllerDropUnitTests : StageControllerUnitTests
    {
        [Fact]
        public async Task WhenDropIsCalledStageIsDropped()
        {
            const string stageName1 = "first";
            const string stageName2 = "second";

            // Arrange
            var stage1 = await AddMockStage(stageName1);
            var stage2 = await AddMockStage(stageName2);

            // Act
            IActionResult actionResult = await _stageController.Drop(stage1.Id);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
            string displayName = (string)result.GetType().GetProperty("DisplayName").GetValue(result);

            displayName.Should().Be(stageName1);

            _stageContextMock.Object.Stages.Count().Should().Be(1);
            _stageContextMock.Object.Stages.First().Id.Should().Be(stage2.Id);
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
        public void VerifyDropRequiresAuthentication()
        {
            AuthorizationTest.IsAuthorized(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenDropIsCalledWithUnauthorizedUser401IsReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");

            _httpContextMock.WithUser(UserKey + 1);
            // Act
            IActionResult actionResult = await _stageController.Drop(stage.Id);

            // Assert
            actionResult.Should().BeOfType<HttpUnauthorizedResult>();
        }

        [Fact]
        public async Task WhenDropIsCalledAndStageIsCommiting400IsReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            AddMockPackage(stage, "package");
            await _stageController.Commit(stage.Id);

            // Act
            IActionResult actionResult = await _stageController.Drop(stage.Id);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}