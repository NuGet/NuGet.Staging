// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface ICommitStatusService
    {
        /// <summary>
        /// Gets the latest commit of the stage.
        /// </summary>
        StageCommit GetCommit(string stageId);




    }
}