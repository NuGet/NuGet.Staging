// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Client.Staging
{
    public class StageView
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Feed { get; set; }
    }
}