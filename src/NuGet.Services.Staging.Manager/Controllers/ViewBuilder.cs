// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Services.Staging.Database.Models;
using NuGet.Client.Staging;

namespace NuGet.Services.Staging.Manager.Controllers
{
    public static class ViewBuilder
    {
        public static StageListView CreateStageListView(Stage stage, StageMembership membership, string baseAddress)
        {
            var view = new StageListView();
            PopulateStageView(view, stage, baseAddress);
            view.MembershipType = membership.MembershipType.ToString();

            return view;
        }

        public static StageDetailedView CreateStageDetailedView(Stage stage, string baseAddress)
        {
            var view = new StageDetailedView();
            PopulateStageView(view, stage, baseAddress);
            view.Packages = new List<PackageView>(stage.Packages.Select(package => CreatePackageView(package)));
            view.PackagesCount = view.Packages.Count;
            view.Memberships = new List<MembershipView>(stage.Memberships.Select(membership => CreateMembershipView(membership)));

            return view;
        }

        public static StageView CreateStageView(Stage stage, string baseAddress)
        {
            var view = new StageView();
            PopulateStageView(view, stage, baseAddress);
            return view;
        }

        public static PackageView CreatePackageView(StagedPackage package)
        {
            return new PackageView
            {
                Id = package.Id,
                Version = package.Version
            };
        }

        public static MembershipView CreateMembershipView(StageMembership membership)
        {
            return new MembershipView
            {
                Name = membership.UserKey.ToString(),
                MembershipType = membership.MembershipType.ToString()
            };
        }
       
        public static StageCommitProgressView CreateStageCommitProgressView(Stage stage, string baseAddress)
        {
            var view = new StageCommitProgressView();
            PopulateStageView(view, stage, baseAddress);

            return view;
        }

        private static void PopulateStageView(StageView stageView, Stage stage, string baseAddress)
        {
            stageView.Id = stage.Id;
            stageView.DisplayName = stage.DisplayName;
            stageView.CreationDate = stage.CreationDate;
            stageView.ExpirationDate = stage.ExpirationDate;
            stageView.Status = stage.Status.ToString();
            stageView.Feed = $"{baseAddress}/api/stage/{stage.Id}/index.json";
        }
    }
}
