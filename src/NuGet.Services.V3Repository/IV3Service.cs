// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace NuGet.Services.V3Repository
{
    public interface IV3Service
    {
        IPackageMetadata ParsePackageStream(Stream stream);

        Task<PackageUris> AddPackage(Stream stream, IPackageMetadata metadata);
    }

    public class PackageUris
    {
        public Uri Nupkg { get; set; }
        public Uri Nuspec { get; set; }
    }
}
