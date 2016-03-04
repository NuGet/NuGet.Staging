// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.OptionsModel;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.V3;

namespace Stage.Manager
{
    public class V3ServiceFactory : IV3ServiceFactory
    {
        private readonly IOptions<V3ServiceOptions> _options;
        private readonly StorageFactory _storageFactory;

        public V3ServiceFactory(IOptions<V3ServiceOptions> options, StorageFactory storageFactory)
        {
            _options = options;
            _storageFactory = storageFactory;
        }

        public IV3Service Create(string stageName)
        {
            return new V3Service(_options, new AppendingStorageFactory(_storageFactory, stageName));
        }
    }
}
