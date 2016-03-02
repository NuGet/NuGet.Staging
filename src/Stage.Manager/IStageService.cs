// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Stage.Manager
{
    public interface IStageService
    {
        Database.Models.Stage GetStage(string stageId);

        bool DoesPackageExistsOnStage(Database.Models.Stage stage, string registrationId, string version);

        bool IsUserMemberOfStage(Database.Models.Stage stage, int userKey);
    }
}