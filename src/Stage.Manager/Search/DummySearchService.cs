// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stage.Database.Models;

namespace Stage.Manager.Search
{
    public class DummySearchService : ISearchService
    {
        private readonly IStageService _stageService;

        public Uri BaseAddress { get; set; }

        public DummySearchService(IStageService stageService)
        {
            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            _stageService = stageService;
        }

        public Task<JObject> Search(string stageId, string query)
        {
            var stage = _stageService.GetStage(stageId);
            JArray dataItems = new JArray();

            if (stage != null)
            {
                foreach (var package in stage.Packages)
                {
                    var item = GetPackageItem(package);
                    dataItems.Add(item);
                }
            }

            var obj = new JObject
            {
                {
                    "@context", new JObject
                    {
                        {
                            "@vocab", "http://schema.nuget.org/schema#"
                        }
                    }
                },
                {
                    "data", dataItems
                }
            };

            return Task.FromResult(obj);
        }

        private JObject GetPackageItem(StagedPackage package)
        {
            Uri registrationBaseAddress = new Uri(BaseAddress, $"{Constants.RegistrationFolderName}/");

            return new JObject
            {
                {"@id", GetRegistrationVersionUri(package, registrationBaseAddress).ToString()},
                {"@type", "Package"},
                {"authors", new JArray()},
                {"description", "Dummy"},
                {"iconUrl", "http://www.your-army.com/img/our-clients/dummy.png"},
                {"id", package.Id},
                {"licenseUrl", ""},
                {"projectUrl", ""},
                {"registration", GetRegistrationIndexUri(package, registrationBaseAddress).ToString()},
                {"tags", new JArray()},
                {"title", package.Id},
                {"totalDownloads", 9999},
                {"version", package.NormalizedVersion},
                {
                    "versions", new JArray
                    {
                        new JObject
                        {
                            {"@id", GetRegistrationVersionUri(package, registrationBaseAddress).ToString()},
                            {"downloads", 9999},
                            {"version", package.NormalizedVersion}
                        }
                    }
                }
            };
        }

        private Uri GetRegistrationVersionUri(StagedPackage package, Uri registrationBaseAddress)
        {
            return new Uri(registrationBaseAddress, $"{package.Id.ToLowerInvariant()}/{package.NormalizedVersion}.json");
        }

        private Uri GetRegistrationIndexUri(StagedPackage package, Uri registrationBaseAddress)
        {
            return new Uri(registrationBaseAddress, $"{package.Id.ToLowerInvariant()}/index.json");
        }
    }
}