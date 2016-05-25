// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NuGet.Services.Staging.Authentication;

namespace NuGet.Services.Staging.Manager.Authentication
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiKeyAuthenticationService _authenticationService;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ApiKeyAuthenticationService authenticationService)
        {
            _next = next;
            _authenticationService = authenticationService;
        }

        public async Task Invoke(HttpContext context)
        {
            var credentials = GetCredentials(context.Request);
            var userInfo = await _authenticationService.Authenticate(credentials);
            context.User = userInfo != null ?
                new ClaimsPrincipal(new NuGetIdentity(userInfo.UserKey, "ApiKey")) :
                new ClaimsPrincipal();
            context.Items[Constants.UserInformationKey] = userInfo;

            await _next.Invoke(context);
        }

        public ApiKeyCredentials GetCredentials(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var apiKey = request.Headers["X-NuGet-ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            return new ApiKeyCredentials
            {
                ApiKey = apiKey
            };
        }
    }

    public class NuGetIdentity : IIdentity
    {
        public NuGetIdentity()
        {
            IsAuthenticated = false;
        }

        public NuGetIdentity(int userKey, string authenticationType)
        {
            Name = userKey.ToString();
            AuthenticationType = authenticationType;
            IsAuthenticated = true;
        }

        public string Name { get; }
        public string AuthenticationType { get; }
        public bool IsAuthenticated { get; }
    }
}