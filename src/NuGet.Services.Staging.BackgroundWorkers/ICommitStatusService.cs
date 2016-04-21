// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface ICommitStatusService
    {
        /// <summary>
        /// Gets the latest commit of the stage.
        /// </summary>
        StageCommit GetCommit(string stageId);

        // dont forget to update commit statys and last update timestamp
        Task  UpdateProgress(StageCommit commit, BatchPushProgressReport progressReport);
    }
}