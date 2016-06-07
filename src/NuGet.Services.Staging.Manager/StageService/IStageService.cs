// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.Manager
{
    public interface IStageService
    {
        /// <summary>
        /// Create a new stage and update DB.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="userKey">The creating user.</param>
        /// <returns>The new stage.</returns>
        Task<Stage> CreateStage(string displayName, int userKey);

        /// <summary>
        /// Retrieves a stage.
        /// </summary>
        Stage GetStage(string stageId);

        /// <summary>
        /// Removes a stage.
        /// </summary>
        Task DropStage(Stage stage);

        /// <summary>
        /// Checks if a package exists on stage.
        /// </summary>
        bool DoesPackageExistOnStage(Stage stage, string registrationId, string version);

        /// <summary>
        /// Retrieve user memberships.
        /// </summary>
        IEnumerable<StageMembership> GetUserMemberships(int userKey);

        /// <summary>
        /// Is the user an owner/contributor of this stage
        /// </summary>
        bool IsStageMember(Stage stage, int userKey);

        /// <summary>
        /// Is the display name valid (non empty and not too long)
        /// </summary>
        bool CheckStageDisplayNameValidity(string displayName);

        /// <summary>
        /// Is pushing/deleting packages from this stage allowed
        /// </summary>
        bool IsStageEditAllowed(Stage stage);

        /// <summary>
        /// Commit stage (DB changes)
        /// </summary>
        Task<StageCommit> CommitStage(Stage stage);

        /// <summary>
        /// Get the latest commit of the stage.
        /// </summary>
        StageCommit GetCommit(Stage stage);

        /// <summary>
        /// Change commit status to failed.
        /// </summary>
        Task SetCommitStatusToFailed(Stage stage, StageCommit commit);

        /// <summary>
        /// Get progress report for this commit.
        /// </summary>
        BatchPushProgressReport GetCommitProgress(StageCommit commit);


        /// <summary>
        /// Adds a package to the stage
        /// </summary>
        Task AddPackageToStage(Stage stage, StagedPackage package);
    }
}