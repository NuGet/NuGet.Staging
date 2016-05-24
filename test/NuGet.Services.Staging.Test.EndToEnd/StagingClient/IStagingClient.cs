// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    interface IStagingClient
    {
        Task<JArray> ListUserStages(string apiKey);

        Task<JObject> GetDetails(string stageId);

        Task<JObject> CreateStage(string displayName, string apiKey);

        Task<JObject> DropStage(string stageId, string apiKey);

        Task CommitStage(string stageId, string apiKey);

        Task<JObject> GetCommitProgress(string stageId);

        Task<JObject> Index(string stageId);

        Task<JObject> Query(string stageId, string queryString);

        Task PushPackage(string stageId, string apiKey, Stream packageStream);
    }
}
