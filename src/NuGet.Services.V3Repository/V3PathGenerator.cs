// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.V3Repository
{
    public class V3PathGenerator
    {
        public Uri BaseAddress { get; }

        public Uri RegistrationBaseAddress => new Uri($"{BaseAddress}{Constants.RegistrationFolderName}/");

        public Uri FlatContainerBaseAddress => new Uri($"{BaseAddress}{Constants.FlatContainerFolderName}/");

        public V3PathGenerator(Uri v3BaseAddress)
        {
            if (v3BaseAddress == null)
            {
                throw new ArgumentNullException(nameof(v3BaseAddress));
            }

            BaseAddress = v3BaseAddress;
        }

        public Uri GetPackageVersionRegistrationAddress(string packageId, string version)
        {
            return new Uri(RegistrationBaseAddress, $"{packageId.ToLowerInvariant()}/{version.ToLowerInvariant()}.json");
        }

        public Uri GetPackageRegistrationIndexAddress(string packageId)
        {
            return new Uri(RegistrationBaseAddress, $"{packageId.ToLowerInvariant()}/index.json");
        }

    }
}
