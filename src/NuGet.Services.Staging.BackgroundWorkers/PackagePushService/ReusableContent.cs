// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuGet.Services.Staging.BackgroundWorkers
{
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
