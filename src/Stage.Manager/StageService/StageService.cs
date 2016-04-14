// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using Stage.Database.Models;
using Stage.Packages;

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
                Memberships = new List<StageMembership>(new[]
                {
                    new StageMembership()
                    {
                        MembershipType = MembershipType.Owner,
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

        // TODO: improve performance by NOT including all stage data
        public virtual Database.Models.Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Memberships)
                           .Include(s => s.Packages)
                           .Include(s => s.Commits).FirstOrDefault(s => s.Id == stageId && s.Status != StageStatus.Deleted);

        public async Task DropStage(Database.Models.Stage stage)
        {
            // TODO: in the future, just mark the stage as deleted and have a background job perform the actual delete
            // TODO: the stage should be removed from storage as well
            _context.Stages.Remove(stage);
            await _context.SaveChangesAsync();
        }

        public virtual bool DoesPackageExistOnStage(Database.Models.Stage stage, string registrationId, string version)
        {
            return stage.Packages.Any(p => string.Equals(p.Id, registrationId, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(p.NormalizedVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<StageMembership> GetUserMemberships(int userKey)
        {
            return _context.StageMemberships.Where(sm => sm.UserKey == userKey).Include(sm => sm.Stage);
        }

        public virtual bool IsStageMember(Database.Models.Stage stage, int userKey) =>
            stage.Memberships.Any(sm => sm.UserKey == userKey);

        public bool CheckStageDisplayNameValidity(string displayName) =>
            !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= MaxDisplayNameLength;

        public bool IsStageEditAllowed(Database.Models.Stage stage)
        {
            return stage.Status == StageStatus.Active;
        }

        private static string GuidToStageId(Guid guid) => guid.ToString("N");

        public async Task CommitStage(Database.Models.Stage stage, string trackingId)
        {
            stage.Status = StageStatus.Committing;
            stage.Commits.Add(new StageCommit
            {
                RequestTime = DateTime.UtcNow,
                TrackId = trackingId,
                Status = CommitStatus.Pending
            });

            await _context.SaveChangesAsync();
        }

        public StageCommit GetCommit(Database.Models.Stage stage)
        {
            return stage.Commits.OrderByDescending(sc => sc.RequestTime).FirstOrDefault();
        }

        public BatchPushProgressReport GetCommitProgress(StageCommit commit)
        {
            if (string.IsNullOrEmpty(commit.Progress))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<BatchPushProgressReport>(commit.Progress);
        }
    }
}
