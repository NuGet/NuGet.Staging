// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager.Controllers
{
    public class DetailedViewStage : ViewStage
    {
        internal DetailedViewStage(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
        {
            Packages = new List<ViewPackage>(stage.Packages.Select(package => new ViewPackage(package)));
            PackagesCount = Packages.Count;
            Memberships = new List<ViewMembership>(stage.Memberships.Select(membership => new ViewMembership(membership)));
        }

        public int PackagesCount { get; internal set; }
        public List<ViewPackage> Packages { get; internal set; }
        public List<ViewMembership> Memberships { get; internal set; }
    }

    public class ListViewStage : ViewStage
    {
        internal ListViewStage(Database.Models.Stage stage, StageMembership membership, string baseAddress) : base(stage, baseAddress)
        {
            MembershipType = membership.MembershipType.ToString();
        }

        public string MembershipType { get; internal set; }
    }

    public class ViewStage
    {
        public string Id { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Status { get; internal set; }
        public DateTime CreationDate { get; internal set; }
        public DateTime ExpirationDate { get; internal set; }
        public string Feed { get; internal set; }

        internal ViewStage(Database.Models.Stage stage, string baseAddress)
        {
            Id = stage.Id;
            DisplayName = stage.DisplayName;
            CreationDate = stage.CreationDate;
            ExpirationDate = stage.ExpirationDate;
            Status = stage.Status.ToString();
            Feed = $"{baseAddress}/api/stage/{stage.Id}/index.json";
        }
    }

    public class ViewPackage
    {
        internal ViewPackage(StagedPackage package)
        {
            Id = package.Id;
            Version = package.Version;
        }

        internal ViewPackage()
        {
        }

        public string Id { get; internal set; }
        public string Version { get; internal set; }
    }

    public class ViewMembership
    {
        internal ViewMembership(StageMembership membership)
        {
            Name = membership.UserKey.ToString();
            MembershipType = membership.MembershipType.ToString();
        }

        // TODO: now this is user key, but change to actual user name
        public string Name { get; internal set; }
        public string MembershipType { get; internal set; }
    }

    public class ViewPackageCommitProgress : ViewPackage
    {
        public string Progress { get; internal set; }
    }

    public class ViewStageCommitProgress : ViewStage
    {
        internal ViewStageCommitProgress(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
        {
        }

        public string CommitStatus { get; internal set; }

        public List<ViewPackageCommitProgress> PackageProgressList { get; internal set; }

        public string ErrorMessage { get; internal set; }
    }
}
