// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Client.Staging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;

namespace NuGet.CommandLine.StagingExtensions
{
    public class StageManagementResource : INuGetResource
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string StagePath = "api/stage";
        private const string MediaType = "application/json";

        private readonly HttpSource _httpSource;
        private readonly Uri _stageServiceUri;

        public StageManagementResource(string stageServiceUri, HttpSource httpSource)
        {
            _stageServiceUri = new Uri(stageServiceUri);
            _httpSource = httpSource;
        }

        public async Task<StageListView> Create(string displayName, string apiKey, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(StagingResources.DisplayNameShouldNotBeEmpty, nameof(displayName));    
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException(StagingResources.ApiKeyShouldNotBeEmpty, nameof(apiKey));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stageServiceUri, StagePath));
                    request.Headers.Add(ApiKeyHeader, apiKey);

                    request.Content = new StringContent("\"" + displayName + "\"", Encoding.UTF8);
                    request.Content.Headers.ContentType.MediaType = MediaType;

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody.FromJson<StageListView>();
                },
                log,
                CancellationToken.None);

            return result;
        }

        public async Task<StageView> Drop(string id, string apiKey, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(StagingResources.StageIdShouldNotBeEmpty, nameof(id));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException(StagingResources.ApiKeyShouldNotBeEmpty, nameof(apiKey));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(_stageServiceUri, $"{StagePath}/{id}"));
                    request.Headers.Add(ApiKeyHeader, apiKey);

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody.FromJson<StageView>();
                },
                log,
                CancellationToken.None);

            return result;
        }

        public async Task<IReadOnlyList<StageListView>> List(string apiKey, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException(StagingResources.ApiKeyShouldNotBeEmpty, nameof(apiKey));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stageServiceUri, StagePath));
                    request.Headers.Add(ApiKeyHeader, apiKey);

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return (IReadOnlyList<StageListView>)JsonConvert.DeserializeObject<IList<StageListView>>(responseBody);
                },
                log,
                CancellationToken.None);

            return result;
        }

        public async Task<StageDetailedView> Get(string id, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(StagingResources.StageIdShouldNotBeEmpty, nameof(id));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stageServiceUri, $"{StagePath}/{id}"));

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<StageDetailedView>(responseBody);
                },
                log,
                CancellationToken.None);

            return result;
        }

        public async Task Commit(string id, string apiKey, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(StagingResources.StageIdShouldNotBeEmpty, nameof(id));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException(StagingResources.ApiKeyShouldNotBeEmpty, nameof(apiKey));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_stageServiceUri, $"{StagePath}/{id}"));
                    request.Headers.Add(ApiKeyHeader, apiKey);

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);
                    return true;
                },
                log,
                CancellationToken.None);
        }

        public async Task<StageCommitProgressView> GetCommitProgress(string id, Common.ILogger log)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(StagingResources.StageIdShouldNotBeEmpty, nameof(id));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_stageServiceUri, $"{StagePath}/{id}/commit"));

                    return request;
                },
                async response =>
                {
                    await EnsureSuccessStatusCode(response);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<StageCommitProgressView>(responseBody);
                },
                log,
                CancellationToken.None);

            return result;
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

                    var errorMessage = $"Response status code does not indicate success: {response.StatusCode}.";

                    if (!string.IsNullOrEmpty(serverMessage))
                    {
                        errorMessage = errorMessage + $" Message: {serverMessage}";
                    }

                    throw new HttpRequestException(errorMessage);
                }
            }
        }
    }
}
