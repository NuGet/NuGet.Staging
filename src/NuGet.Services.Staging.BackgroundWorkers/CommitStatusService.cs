// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class CommitStatusService : ICommitStatusService, IDisposable
    {
        private StageContext _context;

        public CommitStatusService(StageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
        }

        public StageCommit GetCommit(string stageId)
        {
            var stage = _context.Stages.Include(x => x.Commits).FirstOrDefault(x => x.Id == stageId);
            return stage?.Commits.OrderByDescending(sc => sc.RequestTime).FirstOrDefault();
        }

        public async Task UpdateProgress(StageCommit commit, BatchPushProgressReport progressReport)
        {
            commit.Progress = JsonConvert.SerializeObject(progressReport);
            commit.LastProgressUpdate = DateTime.UtcNow;
            commit.Status = PushProgressStatusToCommitStatus(progressReport.Status);

            if (commit.Status == CommitStatus.Completed)
            {
               var stage = _context.Stages.First(x => x.Key == commit.StageKey);
               stage.Status = StageStatus.Committed; 
            }

            await _context.SaveChangesAsync(acceptAllChangesOnSuccess: true);
        }

        private CommitStatus PushProgressStatusToCommitStatus(PushProgressStatus progressStatus)
        {
            switch (progressStatus)
            {
                case PushProgressStatus.Pending: return CommitStatus.Pending;
                case PushProgressStatus.InProgress: return CommitStatus.InProgress;
                case PushProgressStatus.Completed: return CommitStatus.Completed;
                case PushProgressStatus.Failed: return CommitStatus.Failed;
            }

            return CommitStatus.Failed;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
               _context.Dispose();
               _context = null;
            }
        }
    }
}