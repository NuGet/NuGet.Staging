// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.Services.Staging.PackageService
{
    public class PackageBatchPushData
    {
        public string StageId { get; set; }

        public List<PackagePushData> PackagePushDataList { get; set; }
    }
}