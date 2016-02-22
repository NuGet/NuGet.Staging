// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Stage.Database.Models
{
    public class StageMemeber
    {
        public int Key { get; set; }

        public int StageKey { get; set; }

        public int UserKey { get; set; }

        public MemberType MemberType { get; set; }

        public Stage Stage { get; set; }
    }
}