﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.v3;
using NuGet.Services.V3Repository;

namespace NuGet.Services.Staging.Manager.V3
{
    public class StageIndexBuilder
    {
        /// <summary>
        /// Creates an index.json for the stage
        /// </summary>
        public JObject CreateIndex(string baseAddress, string stageId, V3PathGenerator pathGenerator)
        {
            var stageControllerPath = $"{baseAddress}/api/stage/{stageId}";

            var index = new JObject
            {
                {"version", "3.0.0-beta.1"},
                {"@context", new JObject
                    {
                        {"@vocab", @"http://schema.nuget.org/services#"},
                        {"comment", @"http://www.w3.org/2000/01/rdf-schema#comment"}
                    }
                },
                {"resources", new JArray
                    {
                        CreateResource($"{stageControllerPath}/query", ServiceTypes.SearchQueryService[0],  "Search endpoint"),
                        CreateResource($"{stageControllerPath}/query", ServiceTypes.SearchQueryService[1],  "Search endpoint"),
                        CreateResource($"{stageControllerPath}/autocomplete", ServiceTypes.SearchAutocompleteService, "Autocomplete endpoint"),
                        CreateResource(pathGenerator.RegistrationBaseAddress.AbsoluteUri, ServiceTypes.RegistrationsBaseUrl[0], "Registration blobs Uri"),
                        CreateResource(pathGenerator.RegistrationBaseAddress.AbsoluteUri, ServiceTypes.RegistrationsBaseUrl[1], "Registration blobs Uri"),
                        CreateResource(pathGenerator.FlatContainerBaseAddress.AbsoluteUri, ServiceTypes.PackageBaseAddress, "Packages base uri"),
                        CreateResource($"{baseAddress}/api/package/{stageId}", ServiceTypes.PackagePublish, "Package publishing endpoint"),
                    }
                }
            };

            return index;
        }

        private JObject CreateResource(string id, string type, string comment)
        {
            return new JObject
            {
                { "@id", id },
                { "@type", type },
                { "comment", comment } 
            };
        }
    }
}
