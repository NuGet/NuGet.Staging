// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.OptionsModel;
using NuGet.Services.Metadata.Catalog.Dnx;
using NuGet.Services.Metadata.Catalog.Persistence;

namespace Stage.V3
{
    public class V3ServiceOptions
    {
        public string FlatContainerFolderName { get; set; }

        public string RegistrationFolderName { get; set; }

        public string CatalogFolderName { get; set; }
    }

    public class V3Service : IV3Service
    {
        private readonly V3ServiceOptions _options;
        private readonly DnxMaker _dnxMaker;
        private readonly CancellationToken _cancellationToken = new CancellationToken();

        public V3Service(IOptions<V3ServiceOptions> options, StorageFactory storageFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (storageFactory == null)
            {
                throw new ArgumentNullException(nameof(storageFactory));
            }

            _options = options.Value;
            _dnxMaker = new DnxMaker(new AppendingStorageFactory(storageFactory, _options.FlatContainerFolderName));
        }

        public async Task AddPackage(Stream packageStream, string nuspec, string id, string version)
        {
            await _dnxMaker.AddPackage(packageStream, nuspec, id, version, _cancellationToken);
        }
    }
}