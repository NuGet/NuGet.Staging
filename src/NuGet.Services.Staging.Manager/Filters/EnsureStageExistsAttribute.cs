// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace NuGet.Services.Staging.Manager
{
    public class EnsureStageExistsAttribute : ServiceFilterAttribute
    {
        public EnsureStageExistsAttribute() : base(typeof(EnsureStageExistsFilter))
        {
        }
    }
}