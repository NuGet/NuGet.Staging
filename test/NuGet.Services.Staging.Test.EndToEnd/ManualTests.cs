// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class ManualTests
    {
        private readonly ITestOutputHelper _output;
        private readonly StagingEndToEndConfiguration _configuration;

        public ManualTests(ITestOutputHelper output)
        {
            _output = output;
            _configuration = new StagingEndToEndConfiguration();
        }

        /// <summary>
        /// This test creates a stage and pushes 1000 of the most popular nuget packages to it. 
        /// Since the Staging service checks if the package id is available before push,
        /// we alter the package id and append the word "stage" to it before pushing.
        /// </summary>
        /// <returns></returns>
//       [Fact]
        public async Task CreateStageWithPackages()
        {
            const int packagesCount = 1000;

            var client = new StagingClient(new Uri(_configuration.StagingUri), _output);
            var stageCreateResult = await client.CreateStage("MyStage" + DateTime.Now, _configuration.ApiKey);
            string stageId = stageCreateResult[Constants.Stage_Id].ToString();
            _output.WriteLine($"Stage id: {stageId}");

            var nupkgUriList = await GetTopPackages(packagesCount);

            Parallel.ForEach(nupkgUriList, nupkgUri =>
            {
                try
                {
                    using (var stream = GetPackageStream(nupkgUri).Result)
                    {
                        var alteredStream = AlterPackageStream(stream);
                        alteredStream.Seek(0, SeekOrigin.Begin);
                        client.PushPackage(stageId, _configuration.ApiKey, alteredStream).Wait(CancellationToken.None);
                    }
                } 
                catch(Exception e)
                {
                    _output.WriteLine($"Caught exception for package {nupkgUri}. Error: {e}");
                }
            });
        }

        private async Task<Stream> GetPackageStream(Uri nupkgUri)
        {
            var blob = new CloudBlockBlob(nupkgUri);

            var originalStream = new MemoryStream();

            await blob.DownloadToStreamAsync(originalStream, CancellationToken.None);

            originalStream.Seek(0, SeekOrigin.Begin);

            if (blob.Properties.ContentEncoding == "gzip")
            {
                return new GZipStream(originalStream, CompressionMode.Decompress, leaveOpen: false);
            }

            return originalStream;
        }

        private Stream AlterPackageStream(Stream packageStream)
        {
            using (ZipArchive package = new ZipArchive(packageStream, ZipArchiveMode.Update, true))
            {
                var nuspecEntry =
                    package.Entries.First(zipArchiveEntry => zipArchiveEntry.FullName.EndsWith(".nuspec") && zipArchiveEntry.FullName.IndexOf('/') == -1);

                Stream entryStream = nuspecEntry.Open();
                StreamReader reader = new StreamReader(entryStream);
                string content = reader.ReadToEnd();

                content = AlterNuspec(content);
                entryStream.Seek(0, SeekOrigin.Begin);
                StreamWriter writer = new StreamWriter(entryStream);
                writer.Write(content);

                entryStream.SetLength(content.Length);
                writer.Flush();
            }

            return packageStream;
        }

        private string AlterNuspec(string nuspec)
        {
            var xml = XDocument.Parse(nuspec);
            var metadataNode = Enumerable.First(Enumerable.Where(xml.Root.Elements(), e => StringComparer.Ordinal.Equals(e.Name.LocalName, "metadata")));
            var idElement = Enumerable.FirstOrDefault(metadataNode.Elements(XName.Get("id", metadataNode.GetDefaultNamespace().NamespaceName)));
            idElement.Value += "stage";

            return xml.ToString();
        }

        private async Task<IEnumerable<Uri>> GetTopPackages(int count)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api-v2v3search-0.nuget.org/query?take={count}"));

            _output.WriteLine($"Getting {request.RequestUri}");
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var queryResult = JObject.Parse(responseBody);

            var nupkgUriCollection = new ConcurrentBag<Uri>();

            Parallel.ForEach(queryResult["data"], packageData =>
            {
                var versions = packageData["versions"];
                var lastestVersionJsonUri = versions.Last["@id"].ToString();

                var nupkgUri = GetNupkgUri(httpClient, new Uri(lastestVersionJsonUri)).Result;
                nupkgUriCollection.Add(nupkgUri);
            });
            
            return nupkgUriCollection;
        }

        private async Task<Uri> GetNupkgUri(HttpClient httpClient, Uri versionJsonUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, versionJsonUri);
            _output.WriteLine($"Getting {request.RequestUri}");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            var queryResult = JObject.Parse(responseBody);

            return new Uri(queryResult["packageContent"].ToString());
        }
    }
}
