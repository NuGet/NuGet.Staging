﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class AzureReadonlyStorage : IReadonlyStorage
    {
        public async Task<string> ReadAsString(Uri resourceUri)
        {
            var blob = new CloudBlockBlob(resourceUri);

            MemoryStream originalStream = new MemoryStream();
            await blob.DownloadToStreamAsync(originalStream, CancellationToken.None);

            originalStream.Seek(0, SeekOrigin.Begin);

            string content;

            if (blob.Properties.ContentEncoding == "gzip")
            {
                using (var uncompressedStream = new GZipStream(originalStream, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(uncompressedStream))
                    {
                        content = await reader.ReadToEndAsync();
                    }
                }
            }
            else
            {
                using (var reader = new StreamReader(originalStream))
                {
                    content = await reader.ReadToEndAsync();
                }
            }

            return content;
        }
    }
}