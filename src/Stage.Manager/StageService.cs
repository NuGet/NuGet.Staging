// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Entity;
using Stage.Database.Models;

namespace Stage.Manager
{
    public class StageService : IStageService
    {
        private readonly StageContext _context;

        public StageService(StageContext context)
        {
            _context = context;
        }

        public virtual Database.Models.Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Members).Include(s => s.Packages).FirstOrDefault(s => s.Id == stageId);

        public virtual bool DoesPackageExistsOnStage(Database.Models.Stage stage, string registrationId, string version)
        {
            return stage.Packages.Any(p => string.Equals(p.Id, registrationId, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(p.NormalizedVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        public virtual bool IsUserMemberOfStage(Database.Models.Stage stage, int userKey) =>
            stage.Members.Any(sm => sm.UserKey == userKey);
    }
}
