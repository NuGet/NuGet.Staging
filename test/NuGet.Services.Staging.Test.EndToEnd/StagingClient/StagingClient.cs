// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using Xunit.Abstractions;
using NuGet.Protocol.Core.v3;
using System.Linq;
using System.Net;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class StagingClient : IStagingClient
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string StagePath = "api/stage";
        private const string PackagePath = "api/package";

        private readonly Uri _stagingServiceUri;
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _logger;
        private readonly NuGetLoggerAdapter _loggerAdapter;

        public StagingClient(Uri stagingServiceUri, ITestOutputHelper logger)
        {
            if (stagingServiceUri == null)
            {
                throw new ArgumentNullException(nameof(stagingServiceUri));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _stagingServiceUri = stagingServiceUri;
            _logger = logger;
            _loggerAdapter = new NuGetLoggerAdapter(_logger);

            _httpClient = new HttpClient();
        }

        public async Task<JArray> ListUserStages(string apiKey)
        {
            _logger.WriteLine("StagingClient: ListUserStages called.");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, StagePath));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JArray.Parse(responseBody);
        }

        public async Task<JObject> GetDetails(string stageId)
        {
            _logger.WriteLine($"StagingClient: GetDetails called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> CreateStage(string displayName, string apiKey)
        {
            _logger.WriteLine($"StagingClient: CreateStage called with name {displayName}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stagingServiceUri, StagePath));
                request.Headers.Add(ApiKeyHeader, apiKey);

                request.Content = new StringContent("\"" + displayName + "\"", Encoding.UTF8);
                request.Content.Headers.ContentType.MediaType = "application/json";

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> DropStage(string stageId, string apiKey)
        {
            _logger.WriteLine($"StagingClient: DropStage called for stage {stageId}.");

            Func<HttpRequestMessage> requestFactory = () =>
            {

                var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task CommitStage(string stageId, string apiKey)
        {
            _logger.WriteLine($"StagingClient: CommitStage called for stage {stageId}.");

            Func<HttpRequestMessage> requestFactory = () =>
            {

                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            await SendAsync(requestFactory);
        }

        public async Task<JObject> GetCommitProgress(string stageId)
        {
            _logger.WriteLine($"StagingClient: GetCommitProgress called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}/commit"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> Index(string stageId)
        {
            _logger.WriteLine($"StagingClient: Index called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}/index.json"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> Query(string stageId, string queryString)
        {
            _logger.WriteLine($"StagingClient: Query called for stage {stageId}");

            JObject index = await Index(stageId);

            string searchEndpoint = index["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[0]).ToString();

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{searchEndpoint}?q={queryString}"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task PushPackage(string stageId, string apiKey, Stream packageStream)
        {
            _logger.WriteLine($"StagingClient: PushPackage called for stage {stageId}");

            JObject index = await Index(stageId);

            string pushEndpoint = index["resources"].First(x => x["@type"].ToString() == ServiceTypes.PackagePublish)["@id"].ToString();

            var content = new MultipartFormDataContent();
            packageStream.Position = 0;
            var packageContent = new StreamContent(packageStream);
            packageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(packageContent, "package", "package.nupkg");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, new Uri(pushEndpoint));
                
                request.Content = new ReusableContent(content);
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            await SendAsync(requestFactory);
        }

        private async Task<string> SendAsync(Func<HttpRequestMessage> requestFactory)
        {
            var retryHandler = new HttpRetryHandler();

            var response = await retryHandler.SendAsync(
                _httpClient,
                requestFactory,
                HttpCompletionOption.ResponseHeadersRead,
                _loggerAdapter,
                CancellationToken.None);

            await EnsureSuccessStatusCode(response);
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.Content == null)
                {
                    response.EnsureSuccessStatusCode();
                }
                else
                {
                    string serverMessage = await response.Content.ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {response.StatusCode}. Message: {serverMessage}");
                }
            }
        }
    }

    public class ReusableContent : HttpContent
    {
        private readonly HttpContent _innerContent;

        public ReusableContent(HttpContent innerContent)
        {
            _innerContent = innerContent;

            foreach (var header in innerContent.Headers)
            {
                Headers.Add(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _innerContent.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            // Don't call base dispose
            //base.Dispose(disposing);
        }
    }
}