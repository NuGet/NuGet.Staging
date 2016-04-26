// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class StageCommitWorker : IWorker
    {
        private readonly IMessageListener<PackageBatchPushData> _messageListener;
        private readonly IMessageHandlerFactory _messageHandlerFactory;

        public bool IsActive => _messageListener.IsActive;

        public StageCommitWorker(IMessageListener<PackageBatchPushData> messageListener, IMessageHandlerFactory messageHandlerFactory)
        {
            if (messageListener == null)
            {
                throw new ArgumentNullException(nameof(messageListener));
            }

            if (messageHandlerFactory == null)
            {
                throw new ArgumentNullException(nameof(messageHandlerFactory));
            }

            _messageListener = messageListener;
            _messageHandlerFactory = messageHandlerFactory;
        }

        public void Start()
        {
            _messageListener.Start(new InternalMessageHandler(_messageHandlerFactory));
        }

        public void Stop()
        {
            _messageListener.Stop();
        }

        private class InternalMessageHandler : IMessageHandler<PackageBatchPushData> 
        {
            private readonly IMessageHandlerFactory _messageHandlerFactory;

            public InternalMessageHandler(IMessageHandlerFactory messageHandlerFactory)
            {
                if (messageHandlerFactory == null)
                {
                    throw new ArgumentNullException(nameof(messageHandlerFactory));
                }

                _messageHandlerFactory = messageHandlerFactory;
            }

            public async Task HandleMessageAsync(PackageBatchPushData message, bool isLastDelivery)
            {
                // Use a different handler for each message.
                IMessageHandler<PackageBatchPushData> handler = null;

                try
                {
                    handler = _messageHandlerFactory.GetHandler<PackageBatchPushData>();
                    await handler.HandleMessageAsync(message, isLastDelivery);
                }
                finally
                {
                    (handler as IDisposable)?.Dispose();
                }
            }
        } 
    }
}