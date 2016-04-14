// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.Services.Metadata.Catalog.Persistence;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public class TestStorageFactory
        : StorageFactory
    {
        public Dictionary<string, Storage> CreatedStorages { get; }
             
        private readonly Func<string, Storage> _createStorage;

        public TestStorageFactory()
            : this(name => new MemoryStorage())
        {
        }

        public TestStorageFactory(Func<string, Storage> createStorage)
        {
            _createStorage = createStorage;
            BaseAddress = _createStorage(null).BaseAddress;
            CreatedStorages = new Dictionary<string, Storage>();
        }

        public override Storage Create(string name = null)
        {
            var storage = _createStorage(name);
            CreatedStorages[name] = storage;
            return storage;
        }
    }
}
