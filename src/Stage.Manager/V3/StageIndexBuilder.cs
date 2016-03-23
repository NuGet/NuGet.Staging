// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.v3;

namespace Stage.Manager.V3
{
    public class StageIndexBuilder
    {
        /// <summary>
        /// Creates an index.json for the stage
        /// </summary>
        /// <param name="host">The host service.</param>
        /// <param name="scheme">http/https</param>
        /// <param name="stageId">Stage id</param>
        public JObject CreateIndex(string scheme, string host, string stageId)
        {
            var stageFolderPath = $"{scheme}://{host}/{Constants.StagesConatinerName}/{stageId}";

            dynamic index = new JObject
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
                        CreateResource(string.Empty, ServiceTypes.SearchQueryService[0],  "Search endpoint"),
                        CreateResource(string.Empty, ServiceTypes.SearchAutocompleteService, "Autocomplete endpoint"),
                        CreateResource($"{stageFolderPath}/{Constants.RegistrationFolderName}/", ServiceTypes.RegistrationsBaseUrl[0], "Registration blobs Uri"),
                        CreateResource($"{stageFolderPath}/{Constants.FlatContainerFolderName}/", ServiceTypes.PackageBaseAddress, "Packages base uri"),
                        CreateResource($"{scheme}://{host}/api/package/{stageId}", ServiceTypes.PackagePublish, "Package publishing endpoint"),
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
