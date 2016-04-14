// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Mvc;
using NuGet.Services.Staging.Manager.Filters;
using Xunit;

namespace NuGet.Services.Staging.Manager.UnitTests
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
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
            string displayName = (string)result.GetType().GetProperty("DisplayName").GetValue(result);

            displayName.Should().Be(stageName1);

            _stageContextMock.Object.Stages.Count().Should().Be(1);
            _stageContextMock.Object.Stages.First().Id.Should().Be(stage2.Id);
        }

        [Fact]
        public void WhenDropIsCalledWithNonExistingStageId404IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<StageIdFilter>(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void VerifyDropRequiresAuthentication()
        {
            AuthorizationTest.IsAuthorized(_stageController, "Drop", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenDropIsCalledWithUnauthorizedUser401IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<OwnerFilter>(_stageController, "Drop", methodTypes: null).Should().BeTrue();
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