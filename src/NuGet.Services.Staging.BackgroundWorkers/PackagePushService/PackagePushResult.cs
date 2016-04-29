// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class PackagePushResult
    {
        public PackagePushStatus Status { get; internal set; }

        public string ErrorMessage { get; internal set; }
    }
}