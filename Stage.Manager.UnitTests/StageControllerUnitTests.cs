// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Moq;
using Stage.Database.Models;
using Stage.Manager.Controllers;
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
            _stageController = new StageController(new Mock<ILogger<StageController>>().Object, _stageContextMock.Object);
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
            stage.StageMembers.Count().Should().Be(1);
            stage.StageMembers.First().MemberType.Should().Be(MemberType.Owner);
        }

        [Fact]
        public async Task WhenCreateCalledWithInvalidDisplayName500IsReturned()
        {
            // Act 
            IActionResult actionResult = await _stageController.Create("");

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenCreateCalledWithLongDisplayName500IsReturned()
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

            // Act
            IActionResult actionResult = _stageController.GetDetails(secondStageId);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
            string displayName = (string)result.GetType().GetProperty("DisplayName").GetValue(result);

            displayName.Should().Be(_displayName);
        }

        [Fact]
        public void WhenGetDetailsIsCalledWithBadId500IsReturned()
        {
            // Act
            IActionResult actionResult = _stageController.GetDetails("not a guid");

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
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

        public async Task WhenDropIsCalledWithBadId500IsReturned()
        {
            // Act
            IActionResult actionResult = await _stageController.Drop("not a guid");

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task WhenDropIsCalledWithNonExistingStageId404IsReturned()
        {
            // Act
            IActionResult actionResult = await _stageController.Drop(Guid.NewGuid().ToString());

            // Assert
            actionResult.Should().BeOfType<HttpNotFoundResult>();
        }


        private async Task<string> AddMockStage(string displayName)
        {
            IActionResult actionResult = await _stageController.Create(displayName);

            var stage = _stageContextMock.Object.Stages.Last();
            _stageContextMock.Object.StageMembers.AddRange(stage.StageMembers);

            foreach (var member in stage.StageMembers)
            {
                member.Stage = stage;
            }

            object result = (actionResult as HttpOkObjectResult).Value;
            return (string)result.GetType().GetProperty("Id").GetValue(result);
        }
    }
}
