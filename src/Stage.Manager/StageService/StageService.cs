// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Stage.Database.Models;

namespace Stage.Manager
{
    public class StageService : IStageService
    {
        private readonly StageContext _context;

        // After this period the stage will expire and be deleted
        public const int DefaultExpirationPeriodDays = 30;
        public const int MaxDisplayNameLength = 32;

        public StageService(StageContext context)
        {
            _context = context;
        }

        public async Task<Database.Models.Stage> CreateStage(string displayName, int userKey)
        {
            var utcNow = DateTime.UtcNow;

            var stage = new Database.Models.Stage
            {
                Members = new List<StageMember>(new[]
                {
                    new StageMember()
                    {
                        MemberType = MemberType.Owner,
                        UserKey = userKey
                    }
                }),
                Id = GuidToStageId(Guid.NewGuid()),
                DisplayName = displayName,
                CreationDate = utcNow,
                ExpirationDate = utcNow.AddDays(DefaultExpirationPeriodDays),
                Status = StageStatus.Active,
            };

            _context.Stages.Add(stage);
            await _context.SaveChangesAsync();
            return stage;
        }

        public virtual Database.Models.Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Members).Include(s => s.Packages).FirstOrDefault(s => s.Id == stageId);

        public async Task DropStage(Database.Models.Stage stage)
        {
            // TODO: in the future, just mark the stage as deleted and have a background job perform the actual delete
            _context.Stages.Remove(stage);
            await _context.SaveChangesAsync();
        }

        public virtual bool DoesPackageExistsOnStage(Database.Models.Stage stage, string registrationId, string version)
        {
            return stage.Packages.Any(p => string.Equals(p.Id, registrationId, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(p.NormalizedVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        public virtual bool IsUserMemberOfStage(Database.Models.Stage stage, int userKey) =>
            stage.Members.Any(sm => sm.UserKey == userKey);

        public bool CheckStageDisplayNameValidity(string displayName) =>
            !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= MaxDisplayNameLength;

        private static string GuidToStageId(Guid guid) => guid.ToString("N");
    }
}
