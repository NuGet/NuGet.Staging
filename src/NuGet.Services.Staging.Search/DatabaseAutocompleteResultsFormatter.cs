// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Staging.Search
{
    public static class DatabaseAutocompleteResultsFormatter
    {
        public static JObject FormatResults(IEnumerable<string> items, int totalHits)
        {
            return new JObject
            {
                {
                    "@context", new JObject
                    {
                        { "@vocab", "http://schema.nuget.org/schema#" }
                    }
                },
                { "totalHits", totalHits },
                { "lastReopen", DateTime.UtcNow },
                { "index", "DB" },
                {
                    "data", new JArray(items)
                }
            };
        } 
    }
}
