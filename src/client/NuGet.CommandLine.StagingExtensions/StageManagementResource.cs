// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.Staging;
using NuGet.Protocol;
using NuGet.Protocol.Core.v3;

namespace NuGet.CommandLine.StagingExtensions
{
    public class StageManagementResource : Protocol.Core.Types.INuGetResource
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string StagePath = "api/stage";
        private const string MediaType = "application/json";

        private HttpSource _httpSource;
        private string _stageServiceUri;

        public StageManagementResource(string stageServiceUri, HttpSource httpSource)
        {
            if (string.IsNullOrEmpty(stageServiceUri))
            {
                throw new ArgumentNullException(nameof(stageServiceUri));
            }

            if (httpSource == null)
            {
                throw new ArgumentNullException(nameof(httpSource));
            }

            _stageServiceUri = stageServiceUri;
            _httpSource = httpSource;
        }

        public async Task<StageListView> Create(string displayName,string apiKey, Common.ILogger log)
        {
            var result = await _httpSource.ProcessResponseAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_stageServiceUri), StagePath));
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
