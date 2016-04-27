// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Services.Staging.PackageService;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class PackageMetadataService : IPackageMetadataService
    {
        private readonly IReadOnlyStorage _readonlyStorage;
        public PackageMetadataService(IReadOnlyStorage readonlyStorage)
        {
            if (readonlyStorage == null)
            {
                throw new ArgumentNullException(nameof(readonlyStorage));
            }

            _readonlyStorage = readonlyStorage;
        }

        public async Task<IEnumerable<PackageDependency>> GetPackageDependencies(PackagePushData packageData)
        {
            // Retries for storage load are build in
            string nuspec = await _readonlyStorage.ReadAsString(new Uri(packageData.NuspecPath));

            using (var nuspecStream = new MemoryStream(Encoding.ASCII.GetBytes(nuspec)))
            {
                var nuspecReader = new NuspecReader(nuspecStream);
                var dependencySet = new HashSet<PackageDependency>();

                foreach (var dependencyGroup in nuspecReader.GetDependencyGroups())
                {
                    dependencySet.AddRange(dependencyGroup.Packages);
                }

                return dependencySet;
            }
        }
    }
}