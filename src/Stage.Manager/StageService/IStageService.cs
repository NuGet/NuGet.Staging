// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Stage.Manager
{
    public interface IStageService
    {
        Task<Database.Models.Stage> CreateStage(string displayName, int userKey);

        Database.Models.Stage GetStage(string stageId);

        Task DropStage(Database.Models.Stage stage);

        bool DoesPackageExistsOnStage(Database.Models.Stage stage, string registrationId, string version);

        bool IsUserMemberOfStage(Database.Models.Stage stage, int userKey);

        bool CheckStageDisplayNameValidity(string displayName);
    }
}