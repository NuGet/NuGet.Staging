﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public interface IReadOnlyStorage
    {
        Task<string> ReadAsString(Uri resourceUri);

        Task<Stream> ReadAsStream(Uri resourceUri);
    }
}