// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.V3Repository;

namespace NuGet.Services.Staging.Manager
{
    public interface IV3ServiceFactory
    {
        IV3Service Create(string stageId);

        V3PathGenerator CreatePathGenerator(string stageId);
    }
}
