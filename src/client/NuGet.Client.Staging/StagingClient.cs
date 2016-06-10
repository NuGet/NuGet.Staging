// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.v3;

namespace NuGet.Client.Staging
{
    public class StagingClient : IStagingClient
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string StagePath = "api/stage";
        private const string MediaType = "application/json";
        private const string ContentType = "application/octet-stream";
        private readonly Uri _stagingServiceUri;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public StagingClient(Uri stagingServiceUri, ILogger logger)
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

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<IList<StageListView>> ListUserStages(string apiKey)
        {
            _logger.LogDebug("StagingClient: ListUserStages called.");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, StagePath));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JsonConvert.DeserializeObject<IList<StageListView>>(responseBody);
        }

        public async Task<StageDetailedView> GetDetails(string stageId)
        {
            _logger.LogDebug($"StagingClient: GetDetails called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JsonConvert.DeserializeObject<StageDetailedView>(responseBody);
        }

        public async Task<StageListView> CreateStage(string displayName, string apiKey)
        {
            _logger.LogDebug($"StagingClient: CreateStage called with name {displayName}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stagingServiceUri, StagePath));
                request.Headers.Add(ApiKeyHeader, apiKey);

                request.Content = new StringContent("\"" + displayName + "\"", Encoding.UTF8);
                request.Content.Headers.ContentType.MediaType = MediaType;

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JsonConvert.DeserializeObject<StageListView>(responseBody);
        }

        public async Task<StageView> DropStage(string stageId, string apiKey)
        {
            _logger.LogDebug($"StagingClient: DropStage called for stage {stageId}.");

            Func<HttpRequestMessage> requestFactory = () =>
            {

                var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JsonConvert.DeserializeObject<StageView>(responseBody);
        }

        public async Task CommitStage(string stageId, string apiKey)
        {
            _logger.LogDebug($"StagingClient: CommitStage called for stage {stageId}.");

            Func<HttpRequestMessage> requestFactory = () =>
            {

                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}"));
                request.Headers.Add(ApiKeyHeader, apiKey);

                return request;
            };

            await SendAsync(requestFactory);
        }

        public async Task<StageCommitProgressView> GetCommitProgress(string stageId)
        {
            _logger.LogDebug($"StagingClient: GetCommitProgress called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}/commit"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JsonConvert.DeserializeObject<StageCommitProgressView>(responseBody);
        }

        public async Task<JObject> Index(string stageId)
        {
            _logger.LogDebug($"StagingClient: Index called for stage {stageId}");

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{StagePath}/{stageId}/index.json"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> Query(string stageId, string query, bool includePrerelease=false, int skip=0, int take=20)
        {
            _logger.LogDebug($"StagingClient: Query called for stage {stageId}");

            JObject index = await Index(stageId);

            string searchEndpoint = index["resources"].First(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[0])["@id"].ToString();

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{searchEndpoint}?q={query}&prerelease={includePrerelease}&skip={skip}&take={take}"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task<JObject> Autocomplete(string stageId, string query, string packageId="", bool includePrerelease=false, int skip=0, int take=20)
        {
            _logger.LogDebug($"StagingClient: Autocomplete called for stage {stageId}");

            var index = await Index(stageId);

            var searchEndpoint = index["resources"].First(x => x["@type"].ToString() == ServiceTypes.SearchAutocompleteService)["@id"].ToString();

            Func<HttpRequestMessage> requestFactory = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stagingServiceUri, $"{searchEndpoint}?q={query}&id={packageId}&prerelease={includePrerelease}&skip={skip}&take={take}"));
                return request;
            };

            var responseBody = await SendAsync(requestFactory);

            return JObject.Parse(responseBody);
        }

        public async Task PushPackage(string stageId, string apiKey, Stream packageStream)
        {
            _logger.LogDebug($"StagingClient: PushPackage called for stage {stageId}");

            JObject index = await Index(stageId);

            string pushEndpoint = index["resources"].First(x => x["@type"].ToString() == ServiceTypes.PackagePublish)["@id"].ToString();

            var content = new MultipartFormDataContent();
            packageStream.Position = 0;
            var packageContent = new StreamContent(packageStream);
            packageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
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
                _logger,
                CancellationToken.None);

            await EnsureSuccessStatusCode(response);
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private static async Task EnsureSuccessStatusCode(HttpResponseMessage response)
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
}