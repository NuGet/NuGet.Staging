// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface IPackageMetadataService
    {
        Task<IEnumerable<PackageDependency>> GetPackageDependencies(PackagePushData packageData);
    }
}