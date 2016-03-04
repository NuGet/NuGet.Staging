// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;

namespace Stage.V3
{
    public interface IV3Service
    {
        Task AddPackage(Stream packageStream, string nuspec, string id, string version);
    }
}
