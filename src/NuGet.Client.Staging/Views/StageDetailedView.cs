// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.Client.Staging
{
    public class StageDetailedView : StageView
    {
        public int PackagesCount { get; set; }
        public List<PackageView> Packages { get; set; }
        public List<MembershipView> Memberships { get; set; }
    }
}