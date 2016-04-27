// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Services.Staging.BackgroundWorkers;

namespace NuGet.Services.Staging.Runner
{
    public class MessageHandlerFactory : IMessageHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MessageHandlerFactory(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }

        public IMessageHandler<T> GetHandler<T>()
        {
            return _serviceProvider.GetService<IMessageHandler<T>>();
        }
    }
}