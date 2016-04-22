// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Mvc;
using NuGet.Services.Staging.Database.Models;
using Xunit;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public class StageControllerCreateUnitTests : StageControllerUnitTests
    {
        [Fact]
        public async Task WhenCreateIsCalledNewStageIsAdded()
        {
            // Act 
            IActionResult actionResult = await _stageController.Create(DisplayName);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();
            _stageContextMock.Object.Stages.Count().Should().Be(1);

            Database.Models.Stage stage = _stageContextMock.Object.Stages.First();

            stage.DisplayName.Should().Be(DisplayName);
            stage.Status.Should().Be(StageStatus.Active);
            stage.Memberships.Count.Should().Be(1);
            stage.Memberships.First().MembershipType.Should().Be(MembershipType.Owner);
            stage.Memberships.First().UserKey.Should().Be(UserKey);
        }

        [Fact]
        public void VerifyCreateRequiresAuthentication()
        {
            AuthorizationTest.IsAuthorized(_stageController, "Create", methodTypes: null).Should().BeTrue();
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
    }
}