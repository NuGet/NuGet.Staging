// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Stage.Database.Models
{
    public class Stage
    {
        public int Key { get; set; }

        public string Id { get; set; }

        public string DisplayName { get; set; }

        public StageStatus Status { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        public List<StageMember> Members { get; set; } 

        public List<StagedPackage> Packages { get; set; } 

        public List<StageCommit> Commits { get; set; } 
    }
}
