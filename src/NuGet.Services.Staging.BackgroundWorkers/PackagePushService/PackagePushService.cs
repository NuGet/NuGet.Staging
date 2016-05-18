// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Protocol;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class PackagePushServiceOptions
    {
        public string PushUri { get; set; }
    }

    public class PackagePushService : IPackagePushService
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";

        private readonly PackagePushServiceOptions _options;
        private readonly ILogger<PackagePushService> _logger;
        private readonly IReadOnlyStorage _readonlyStorage;
        private readonly HttpClient _httpClient;
        private readonly NuGetLoggerAdapter _loggerAdapter;
        private readonly ApiKeyAuthenticationService _authenticationService;

        public PackagePushService(IReadOnlyStorage readOnlyStorage, ApiKeyAuthenticationService authenticationService,
                                  IOptions<PackagePushServiceOptions> options, ILogger<PackagePushService> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (readOnlyStorage == null)
            {
                throw new ArgumentNullException(nameof(readOnlyStorage));
            }

            if (authenticationService == null)
            {
                throw new ArgumentNullException(nameof(authenticationService));
            }

            _authenticationService = authenticationService;
            _readonlyStorage = readOnlyStorage;
            _options = options.Value;
            _logger = logger;
            _loggerAdapter = new NuGetLoggerAdapter(_logger);

            _httpClient = new HttpClient();
        }

        public async Task<PackagePushResult> PushPackage(PackagePushData pushData)
        {
            var result = new PackagePushResult();

            // 1. Get user APiKey
            var credentials = await _authenticationService.GetCredentials(new UserInformation { UserKey = Int32.Parse(pushData.UserKey) });

            _logger.LogTrace("Extracted ApiKey for user {User}", pushData.UserKey);

            // 2. Load package to memory
            using (var packageStream = await GetPackage(pushData.NupkgPath))
            {
                _logger.LogTrace("Loaded package {@Package} to memory.", pushData);

                // 3. Upload to the gallery
                var response = await SendPackage(packageStream, credentials.ApiKey);

                _logger.LogInformation("Pushed package {Id} {Version} to {Feed}. Response: {Response}", pushData.Id, pushData.Version, _options.PushUri, response);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    result.Status = PackagePushStatus.Success;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    result.Status = PackagePushStatus.AlreadyExists;
                    result.ErrorMessage = response.ReasonPhrase;
                }
                else // Failure
                {
                    result.Status = PackagePushStatus.Failure;
                    result.ErrorMessage = response.ReasonPhrase;
                }
            }

            return result;
        }

        private async Task<Stream> GetPackage(string nupkgPath)
        {
            var stream = await _readonlyStorage.ReadAsStream(new Uri(nupkgPath));
            return stream;
        }

        private async Task<HttpResponseMessage> SendPackage(Stream packageStream, string apiKey)
        {
            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, _options.PushUri);
                var content = new MultipartFormDataContent();

                packageStream.Position = 0;

                var packageContent = new StreamContent(packageStream);
                packageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                //"package" and "package.nupkg" are random names for content deserializing
                //not tied to actual package name.  
                content.Add(packageContent, "package", "package.nupkg");
                request.Content = content;
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            var retryHandler = new HttpRetryHandler();
            var handlerRequest = new HttpRetryHandlerRequest(_httpClient, requestFactory);

            var response = await retryHandler.SendAsync(
                handlerRequest,
                _loggerAdapter,
                CancellationToken.None);

            return response;
        }
    }
}