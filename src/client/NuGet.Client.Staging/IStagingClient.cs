// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Staging
{
    public interface IStagingClient
    {
        Task<IReadOnlyList<StageListView>> ListUserStages(string apiKey);

        Task<StageDetailedView> GetDetails(string stageId);

        Task<StageListView> CreateStage(string displayName, string apiKey);

        Task<StageView> DropStage(string stageId, string apiKey);

        Task CommitStage(string stageId, string apiKey);

        Task<StageCommitProgressView> GetCommitProgress(string stageId);

        Task<JObject> Index(string stageId);

        Task<JObject> Query(string stageId, string query, bool includePrerelease, int skip, int take);

        Task<JObject> Autocomplete(string stageId, string query, string packageId, bool includePrerelease, int skip, int take);

        Task PushPackage(string stageId, string apiKey, Stream packageStream);
    }
}
