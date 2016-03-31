// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using Stage.Authentication;

namespace Stage.Manager.Authentication
{
    public class ApiKeyCredentialsExtractor : IAuthenticationCredentialsExtractor
    {
        private readonly ILogger _logger;

        public ApiKeyCredentialsExtractor(ILogger<ApiKeyCredentialsExtractor> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        public ICredentials GetCredentials(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var apiKey = request.Headers["X-NuGet-ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Failed to extract ApiKey. Header was missing");
                return null;
            }

            return new ApiKeyCredentials
            {
                ApiKey = apiKey
            };
        }
    }
}
