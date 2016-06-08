// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace NuGet.Client.Staging
{
    public class StageDetailedView : StageView
    {
        public int PackagesCount { get; set; }
        public List<PackageView> Packages { get; set; }
        public List<MembershipView> Memberships { get; set; }
    }

    public class StageListView : StageView
    {
        public string MembershipType { get; set; }
    }

    public class StageView
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Feed { get; set; }
    }

    public class PackageView
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }

    public class MembershipView
    {
        public string Name { get; set; }
        public string MembershipType { get; set; }
    }

    public class PackageCommitProgressView : PackageView
    {
        public string Progress { get; set; }
    }

    public class StageCommitProgressView : StageView
    {
        public string CommitStatus { get; set; }

        public List<PackageCommitProgressView> PackageProgressList { get; set; }

        public string ErrorMessage { get; set; }
    }
}
