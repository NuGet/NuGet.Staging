// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stage.Database.Models;

namespace Stage.Manager.Controllers
{
    public class DetailedViewStage : ViewStage
    {
        internal DetailedViewStage(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
        {
            Packages = new List<ViewPackage>(stage.Packages.Select(package => new ViewPackage(package)));
            PackagesCount = Packages.Count;
            Members = new List<ViewMember>(stage.Members.Select(member => new ViewMember(member)));
        }

        public int PackagesCount { get; internal set; }
        public List<ViewPackage> Packages { get; internal set; }
        public List<ViewMember> Members { get; internal set; }
    }

    public class ListViewStage : ViewStage
    {
        internal ListViewStage(Database.Models.Stage stage, StageMember member, string baseAddress) : base(stage, baseAddress)
        {
            MemberType = member.MemberType.ToString();
        }

        public string MemberType { get; internal set; }
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

    public class ViewMember
    {
        internal ViewMember(StageMember member)
        {
            Name = member.UserKey.ToString();
            MemberType = member.MemberType.ToString();
        }

        // TODO: now this is user key, but change to actual user name
        public string Name { get; internal set; }
        public string MemberType { get; internal set; }
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
