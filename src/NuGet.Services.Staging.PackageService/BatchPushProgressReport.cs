// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.Services.Staging.PackageService
{
    public class BatchPushProgressReport
    {
        public PushProgressStatus Status { get; set; }

        public List<PackagePushProgressReport> PackagePushProgressReports { get; set; } 
        
        public string FailureDetails { get; set; }
    }

    public class PackagePushProgressReport
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public PushProgressStatus Status { get; set; }
    }

    public enum PushProgressStatus
    {
        Pending,    // Not started yet
        InProgress, // Push in progress
        Completed,  // Push completed
        Failed      // Push failed
    }
}