// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Moq;

namespace Stage.Manager.UnitTests
{
    public static class ControllerExtensions
    {
        public static Mock<HttpContext> SetupUser(this Controller controller, int userKey)
        {
            var actionContext = new ActionContext();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(x => x.User.Identity.Name).Returns(userKey.ToString);
            actionContext.HttpContext = mockHttpContext.Object;
            controller.ActionContext = actionContext;
            return mockHttpContext;
        }
    }
}