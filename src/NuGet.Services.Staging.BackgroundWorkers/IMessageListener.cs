﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface IMessageListener<T> where T : class
    {
        bool IsActive { get; }

        void Start(HandleMessage<T> messageHandler);

        Task Stop();
    }

    public delegate Task HandleMessage<T>(T message, bool isLastDelivery);
}