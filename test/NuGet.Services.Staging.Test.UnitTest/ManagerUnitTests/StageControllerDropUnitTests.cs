// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest
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
            IActionResult actionResult = await _stageController.Drop(stage1);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();

            object result = (actionResult as OkObjectResult).Value;
            string displayName = (string)result.GetType().GetProperty("DisplayName").GetValue(result);

            displayName.Should().Be(stageName1);
            stage1.Status.ShouldBeEquivalentTo(StageStatus.Deleted);
            stage2.Status.ShouldBeEquivalentTo(StageStatus.Active);
        }

        [Fact]
        public void WhenDropIsCalledWithNonExistingStageId404IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<EnsureStageExistsFilter>(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void VerifyDropRequiresAuthentication()
        {
            AuthorizationTest.IsAuthorized(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void WhenDropIsCalledWithUnauthorizedUser401IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<EnsureUserIsOwnerOfStageFilter>(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenDropIsCalledAndStageIsCommiting400IsReturned()
        {
            // Arrange
            var stage = await AddMockStage("stage");
            AddMockPackage(stage, "package");
            await _stageController.Commit(stage);

            // Act
            IActionResult actionResult = await _stageController.Drop(stage);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}