// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Stage.Database.Models
{
    public class StageCommit
    {
        public int Key { get; set; }

        public int StageKey { get; set; }

        public DateTime RequestTime { get; set; }

        public DateTime LastProgressUpdate { get; set; }

        public CommitStatus Status { get; set; }

        public string TrackId { get; set; }
        
        public string Progress { get; set; }

        public string ErrorDetails { get; set; }
    }
}