// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public class EnsureUserIsOwnerOfStageFilterUnitTests
    {
        protected string DefaultStageId = Guid.NewGuid().ToString();
        protected UserInformation DefaultUser = new UserInformation { UserKey = 1, UserName = "testUser" };
        protected EnsureUserIsOwnerOfStageFilter EnsureUserIsOwnerOfStageFilter;
        protected ActionExecutingContext _actionExecutionContext;
        protected Mock<IStageService> _stageServiceMock;


        public EnsureUserIsOwnerOfStageFilterUnitTests()
        {
            _stageServiceMock = new Mock<IStageService>();
            
            var dictionary = new Dictionary<string, object>();
            dictionary[EnsureStageExistsFilter.StageKeyName] = new Stage
            {
                Id = DefaultStageId
            };

            var actionContext = new ActionContext();
            var httpContext = new Mock<HttpContext>()
                                .WithUser(DefaultUser)
                                .WithRegisteredService((sc) => sc.AddSingleton<IStageService>(_stageServiceMock.Object));

            actionContext.HttpContext = httpContext.Object;
            actionContext.RouteData = new Mock<RouteData>().Object;
            actionContext.ActionDescriptor = new Mock<ActionDescriptor>().Object;

            _actionExecutionContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), dictionary, null);

            EnsureUserIsOwnerOfStageFilter = new EnsureUserIsOwnerOfStageFilter();
        }

        [Fact]
        public void WhenUserIsOwnerFilterSucceeds()
        {
            // Arrange
            _stageServiceMock.Setup(x =>
                                    x.IsStageMember((Stage)_actionExecutionContext.ActionArguments[EnsureStageExistsFilter.StageKeyName], DefaultUser.UserKey))
                              .Returns(true);

            // Act
            EnsureUserIsOwnerOfStageFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            Assert.Null(_actionExecutionContext.Result);
        }

        [Fact]
        public void WhenUserIsNotOwnerFilterReturns403()
        {
            // Arrange
            _stageServiceMock.Setup(x =>
                                    x.IsStageMember((Stage)_actionExecutionContext.ActionArguments[EnsureStageExistsFilter.StageKeyName], DefaultUser.UserKey))
                              .Returns(false);

            // Act
            EnsureUserIsOwnerOfStageFilter.OnActionExecuting(_actionExecutionContext);

            // Assert
            _actionExecutionContext.Result.Should().BeOfType<UnauthorizedResult>();
        }
    }
}