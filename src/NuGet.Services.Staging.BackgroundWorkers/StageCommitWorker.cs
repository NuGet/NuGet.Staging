// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class StageCommitWorker : IWorker
    {
        private readonly IMessageListener<PackageBatchPushData> _messageListener;
        private readonly ILogger<StageCommitWorker> _logger;
        private readonly ICommitStatusService _commitStatusService;

        public bool IsActive => _messageListener.IsActive;

        public StageCommitWorker(IMessageListener<PackageBatchPushData> messageListener, ICommitStatusService commitStatusService,
            ILogger<StageCommitWorker> logger)
        {
            if (messageListener == null)
            {
                throw new ArgumentNullException(nameof(messageListener));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (commitStatusService == null)
            {
                throw new ArgumentNullException(nameof(commitStatusService));
            }

            _messageListener = messageListener;
            _logger = logger;
            _commitStatusService = commitStatusService;
        }

        public void Start()
        {
            _messageListener.Start(HandleBatchPushRequest);
        }

        public void Stop()
        {
            _messageListener.Stop();
        }

        internal async Task HandleBatchPushRequest(PackageBatchPushData pushData)
        {
            /*
             * Get commit information from the DB. If commit failed, discard the message.
             * Create status dictionary: package to upload status. 
             * Sort packages by dependency order
             * for each package:
             * 1. if status is "in progress": try to push. if fails on conflict, ignore the failure. Update the status to completed (in the DB).
             * 2. if status is "pending": try to push. if fails (with retries), mark the status as failed and exit. if success, mark success (DB) and continue.
             * 
             * Once all packages are uploaded mark status as completed.
             */










            _logger.LogInformation("Got message for {StageId}", pushData.StageId);
        }
    }
}