// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Routing;
using Moq;
using Stage.Manager.Filters;
using Xunit;

namespace Stage.Manager.UnitTests
{
    public class OwnerFilterUnitTests
    {
        protected string DefaultStageId = Guid.NewGuid().ToString();
        protected int DefaultUserKey = 1;
        protected OwnerFilter _OwnerFilter;
        protected ActionExecutingContext _actionExecutionContext;
        protected Mock<IStageService> _stageServiceMock;


        public OwnerFilterUnitTests()
        {
            _stageServiceMock = new Mock<IStageService>();
            
            var dictionary = new Dictionary<string, object>();
            dictionary[StageIdFilter.StageKeyName] = new Database.Models.Stage
            {
                Id = DefaultStageId
            };

            var actionContext = new ActionContext();
            var httpContext = new Mock<HttpContext>().WithUser(DefaultUserKey);
            actionContext.HttpContext = httpContext.Object;
            actionContext.RouteData = new Mock<RouteData>().Object;
            actionContext.ActionDescriptor = new Mock<ActionDescriptor>().Object;

            _actionExecutionContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), dictionary, null);
            _OwnerFilter = new OwnerFilter(_stageServiceMock.Object);
        }

        [Fact]
        public void WhenUserIsOwnerFilterSucceeds()
        {
            // Arrange
            _stageServiceMock.Setup(x =>
                                    x.IsStageMember((Database.Models.Stage)_actionExecutionContext.ActionArguments[StageIdFilter.StageKeyName], DefaultUserKey))
                              .Returns(true);

            // Act
            _OwnerFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            Assert.Null(_actionExecutionContext.Result);
        }

        [Fact]
        public void WhenUserIsNotOwnerFilterReturns403()
        {
            // Arrange
            _stageServiceMock.Setup(x =>
                                    x.IsStageMember((Database.Models.Stage)_actionExecutionContext.ActionArguments[StageIdFilter.StageKeyName], DefaultUserKey))
                              .Returns(false);

            // Act
            _OwnerFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            _actionExecutionContext.Result.Should().BeOfType<HttpUnauthorizedResult>();
        }
    }
}