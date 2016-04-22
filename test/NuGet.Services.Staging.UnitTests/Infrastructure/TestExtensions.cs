// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Moq;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public static class TestExtensions
    {
        public static Mock<HttpContext> WithMockHttpContext(this Controller controller)
        {
            var mockHttpContext = new Mock<HttpContext>();
            controller.ActionContext.HttpContext = mockHttpContext.Object;
            return mockHttpContext;
        }

        public static Mock<HttpContext> WithUser(this Mock<HttpContext> httpContextMock, int userKey)
        {
            httpContextMock.SetupGet(x => x.User.Identity.Name).Returns(userKey.ToString);
            return httpContextMock;
        }

        public static Mock<HttpContext> WithBaseAddress(this Mock<HttpContext> httpContextMock)
        {
            var mockRequest = new Mock<HttpRequest>();

            mockRequest.Setup(x => x.Scheme).Returns("http");
            mockRequest.Setup(x => x.Host).Returns(new HostString("stage.nuget.org"));
            httpContextMock.Setup(x => x.Request).Returns(mockRequest.Object);
            return httpContextMock;
        }

        public static Mock<HttpContext> WithFile(this Mock<HttpContext> httpContextMock, Stream stream)
        {
            var mockRequest = new Mock<HttpRequest>();
            var mockForm = new Mock<IFormCollection>();
            var formFileCollection = new Mock<IFormFileCollection>();
            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(x => x.OpenReadStream()).Returns(stream);
            formFileCollection.Setup(x => x[It.IsAny<int>()]).Returns(formFileMock.Object);
            mockForm.Setup(x => x.Files).Returns(formFileCollection.Object);
            mockRequest.Setup(x => x.Form).Returns(mockForm.Object);
            httpContextMock.Setup(x => x.Request).Returns(mockRequest.Object);
            return httpContextMock;
        }
    }
}