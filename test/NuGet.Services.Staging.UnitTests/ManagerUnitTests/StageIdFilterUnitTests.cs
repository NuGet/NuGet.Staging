// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Routing;
using Moq;
using NuGet.Services.Staging.Manager.Filters;
using Xunit;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public class StageIdFilterUnitTests
    {
        private string DefaultStageId = Guid.NewGuid().ToString();
        private StageIdFilter _stageIdFilter;
        private ActionExecutingContext _actionExecutionContext;
        private Mock<IStageService> _stageServiceMock;


        public StageIdFilterUnitTests()
        {
            _stageServiceMock = new Mock<IStageService>();

            var dictionary = new Dictionary<string, object>();
            dictionary[StageIdFilter.StageKeyName] = new Database.Models.Stage
            {
                Id = DefaultStageId
            };

            var actionContext = new ActionContext();
            actionContext.HttpContext = new Mock<HttpContext>().Object;
            actionContext.RouteData = new Mock<RouteData>().Object;
            actionContext.ActionDescriptor = new Mock<ActionDescriptor>().Object;

            _actionExecutionContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), dictionary, null);
            _stageIdFilter = new StageIdFilter(_stageServiceMock.Object);
        }

        [Fact]
        public void WhenStageExistsStageIsSet()
        {
            // Arrange
            var stage = new Database.Models.Stage
            {
                Id = DefaultStageId,
                DisplayName = "some name"
            };

            _stageServiceMock.Setup(x => x.GetStage(DefaultStageId)).Returns(stage);

            // Act
            _stageIdFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            var updatedStage = (Database.Models.Stage)_actionExecutionContext.ActionArguments[StageIdFilter.StageKeyName];
            updatedStage.DisplayName.Should().Be(stage.DisplayName);
        }

        [Fact]
        public void WhenStageDoesNotExist404IsReturned()
        {
            // Act
            _stageIdFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            _actionExecutionContext.Result.Should().BeOfType<HttpNotFoundResult>();
        }
    }
}
