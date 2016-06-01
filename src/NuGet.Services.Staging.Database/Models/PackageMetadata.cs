// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Staging.Database.Models
{
    public class PackageMetadata
    {
        public int Key { get; set; }
        public int StageKey { get; set; }
        public int StagedPackageKey { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Authors { get; set; }
        public string Owners { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string Tags { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public bool IsPrerelease { get; set; }

        public Stage Stage { get; set; }
        public StagedPackage StagedPackage { get; set; }
    }
}
