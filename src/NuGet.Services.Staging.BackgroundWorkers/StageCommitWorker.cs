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

        public bool IsActive => _messageListener.IsActive;

        public StageCommitWorker(IMessageListener<PackageBatchPushData> messageListener, ILogger<StageCommitWorker> logger)
        {
            if (messageListener == null)
            {
                throw new ArgumentNullException(nameof(messageListener));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _messageListener = messageListener;
            _logger = logger;
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
            _logger.LogInformation("Got message for {StageId}", pushData.StageId);
        }
    }
}