// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Stage.Packages
{
    public class PackagePushData
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string NupkgPath { get; set; }
        public string UserKey { get; set; }
    }
}