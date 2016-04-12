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
        public DetailedViewStage(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
        {
            Packages = new List<ViewPackage>(stage.Packages.Select(package => new ViewPackage(package)));
            PackagesCount = Packages.Count;
            Members = new List<ViewMember>(stage.Members.Select(member => new ViewMember(member)));
        }

        public int PackagesCount { get; set; }
        public List<ViewPackage> Packages { get; set; }
        public List<ViewMember> Members { get; set; }
    }

    public class ListViewStage : ViewStage
    {
        public ListViewStage(Database.Models.Stage stage, StageMember member, string baseAddress) : base(stage, baseAddress)
        {
            MemberType = member.MemberType.ToString();
        }

        public string MemberType { get; set; }
    }

    public class ViewStage
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Feed { get; set; }

        public ViewStage(Database.Models.Stage stage, string baseAddress)
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
        public ViewPackage(StagedPackage package)
        {
            Id = package.Id;
            Version = package.Version;
        }

        public ViewPackage()
        {
        }

        public string Id { get; set; }
        public string Version { get; set; }
    }

    public class ViewMember
    {
        public ViewMember(StageMember member)
        {
            Name = member.UserKey.ToString();
            MemberType = member.MemberType.ToString();
        }

        // TODO: now this is user key, but change to actual user name
        public string Name { get; set; }
        public string MemberType { get; set; }
    }

    public class ViewPackageCommitProgress : ViewPackage
    {
        public string Progress { get; set; }
    }

    public class ViewStageCommitProgress : ViewStage
    {
        public ViewStageCommitProgress(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
        {
        }

        public string CommitStatus { get; set; }

        public List<ViewPackageCommitProgress> PackageProgressList { get; set; }

        public string ErrorMessage { get; set; }
    }
}
