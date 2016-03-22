// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using NuGet.Services.Metadata.Catalog.Persistence;

namespace Stage.V3
{
    public class AppendingStorageFactory : StorageFactory
    {
        private readonly StorageFactory _innerFactory;
        private readonly string _pathToAppend;

        public AppendingStorageFactory(StorageFactory factory, string pathToAppend)
        {
            _innerFactory = factory;
            _pathToAppend = pathToAppend;
            BaseAddress = new Uri($"{factory.BaseAddress}/{_pathToAppend}");
        }

        public override Storage Create(string name = null)
        {
            return _innerFactory.Create($"{_pathToAppend}/{name}");
        }
    }
}
