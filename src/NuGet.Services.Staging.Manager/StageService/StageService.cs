// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.Manager
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

        public async Task<Stage> CreateStage(string displayName, int userKey)
        {
            var utcNow = DateTime.UtcNow;

            var stage = new Stage
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
        public virtual Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Memberships)
                           .Include(s => s.Packages)
                           .Include(s => s.Commits).FirstOrDefault(s => s.Id == stageId && s.Status != StageStatus.Deleted);

        public async Task DropStage(Stage stage)
        {
            stage.Status = StageStatus.Deleted;
            await _context.SaveChangesAsync();
        }

        public virtual bool DoesPackageExistOnStage(Stage stage, string registrationId, string version)
        {
            return stage.Packages.Any(p => string.Equals(p.Id, registrationId, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(p.NormalizedVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<StageMembership> GetUserMemberships(int userKey)
        {
            return _context.StageMemberships.Where(sm => sm.UserKey == userKey && sm.Stage.Status != StageStatus.Deleted).Include(sm => sm.Stage);
        }

        public virtual bool IsStageMember(Stage stage, int userKey) =>
            stage.Memberships.Any(sm => sm.UserKey == userKey);

        public bool CheckStageDisplayNameValidity(string displayName) =>
            !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= MaxDisplayNameLength;

        public bool IsStageEditAllowed(Stage stage)
        {
            return stage.Status == StageStatus.Active;
        }

        private static string GuidToStageId(Guid guid) => guid.ToString("N");

        public async Task CommitStage(Stage stage, string trackingId)
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

        public StageCommit GetCommit(Stage stage)
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

        public Task AddPackageToStage(Stage stage, StagedPackage package)
        {
            stage.Packages.Add(package);
            return _context.SaveChangesAsync();
        }
    }
}
