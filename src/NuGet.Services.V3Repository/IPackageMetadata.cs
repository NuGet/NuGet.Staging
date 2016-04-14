// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;

namespace NuGet.Services.V3Repository
{
    public interface IPackageMetadata
    {
        XDocument Nuspec { get; }
    }
}
