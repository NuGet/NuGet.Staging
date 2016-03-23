// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.V3Repository;
using Stage.V3;

namespace Stage.Manager
{
    public class V3ServiceFactory : IV3ServiceFactory
    {
        private readonly V3ServiceOptions _options;
        private readonly StorageFactory _storageFactory;
        private readonly ILogger<V3Service> _logger;

        public V3ServiceFactory(IOptions<V3ServiceOptions> options, StorageFactory storageFactory, ILogger<V3Service> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (storageFactory == null)
            {
                throw new ArgumentNullException(nameof(storageFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _options = options.Value;
            _storageFactory = storageFactory;
            _logger = logger;
        }

        public IV3Service Create(string stageName)
        {
            return new V3Service(_options, new AppendingStorageFactory(_storageFactory, stageName), _logger);
        }
    }
}
