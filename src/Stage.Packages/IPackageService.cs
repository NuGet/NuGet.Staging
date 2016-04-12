﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Stage.Packages
{
    public interface IPackageService
    {
        Task<bool> DoesPackageExistsAsync(string id, string version);

        Task<bool> IsUserOwnerOfPackageAsync(int userKey, string packageId);

        /// <summary>
        /// Pushes a batch of packages.
        /// </summary>
        /// <returns>Tracking id that allows monitoring the progress of the push.</returns>
        Task<string> PushBatchAsync(PackageBatchPushData data);
    }
}