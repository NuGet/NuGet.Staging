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
        private readonly IMessageProvider<PackageBatchPushData> _messageProvider;
        private readonly ILogger<StageCommitWorker> _logger;

        public bool IsActive => _messageProvider.IsActive;

        public StageCommitWorker(IMessageProvider<PackageBatchPushData> messageProvider, ILogger<StageCommitWorker> logger)
        {
            if (messageProvider == null)
            {
                throw new ArgumentNullException(nameof(messageProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _messageProvider = messageProvider;
            _logger = logger;
        }

        public void Start()
        {
            _messageProvider.Start(HandleBatchPushRequest);
        }

        public void Stop()
        {
            _messageProvider.Stop();
        }

        internal async Task HandleBatchPushRequest(PackageBatchPushData pushData)
        {
            _logger.LogInformation("Got message. Stage Id: {0}", pushData.StageId);
        }
    }
}