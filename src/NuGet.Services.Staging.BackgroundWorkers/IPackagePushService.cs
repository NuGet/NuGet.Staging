// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface IPackagePushService
    {
        Task<PackagePushResult> PushPackage(PackagePushData pushData);
    }

    public class EmptyPackagePushService : IPackagePushService
    {
        public Task<PackagePushResult> PushPackage(PackagePushData pushData)
        {
            return Task.FromResult(new PackagePushResult
            {
                Status = PackagePushStatus.Success
            });
        }
    }

    public class PackagePushResult
    {
        public PackagePushStatus Status { get; set; }

        public string ErrorMessage { get; set; }
    }

    public enum PackagePushStatus
    {
        Success,
        AlreadyExists,
        Failure,
    }
}