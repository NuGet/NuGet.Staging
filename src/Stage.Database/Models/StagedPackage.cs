// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Stage.Database.Models
{
    public class StagedPackage
    {
        public int Key { get; set; }

        public string Id { get; set; }

        public string Version { get; set; }

        public string NormalizedVersion { get; set; } 

        public int StageKey { get; set; }

        public int UserKey { get; set; }

        public DateTime Published { get; set; }

        public string NupkgUrl { get; set; }
    }
}
