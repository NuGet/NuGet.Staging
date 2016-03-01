// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Stage.Manager
{
    public static class StageHelper
    {
        public static bool VerifyStageId(string id)
        {
            Guid stageIdGuid;
            return Guid.TryParse(id, out stageIdGuid);
        }

        public static bool IsUserMemberOfStage(this Database.Models.Stage stage, int userKey) =>
            stage.Members.Any(sm => sm.UserKey == userKey);
    }
}
