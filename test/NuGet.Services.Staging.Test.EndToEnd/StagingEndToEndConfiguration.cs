// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class StagingEndToEndConfiguration
    {
        public string ApiKey { get; set; }
        public int PackagesToPushCount { get; set; }
        public int CommitTimeoutInMinutes { get; set; }
        public string StagingUri { get; set; }

        public StagingEndToEndConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("testsettings.json");

            var configurationRoot = builder.Build();

            configurationRoot.GetSection("StagingEndToEnd").Bind(this);
        }
    }
}