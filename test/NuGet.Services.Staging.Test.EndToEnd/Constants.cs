// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public static class Constants
    {
        public const string Stage_Id = "Id";
        public const string Stage_Packages = "Packages";
        public const string Stage_Status = "Status";
        public const string Stage_DisplayName = "DisplayName";
        public const string Stage_MembershipType = "MembershipType";
        public const string Stage_PackageCount = "PackagesCount";

        public const string CommitProgress_CommitStatus = "CommitStatus";
        public const string CommitProgress_ErrorMessage = "ErrorMessage";
        public const string CommitProgress_PackageProgressList = "PackageProgressList";

        public const string Package_Version = "Version";
        public const string Package_Progress = "Progress";

        public const string CommitStatus_Failed = "Failed";
        public const string CommitStatus_Completed = "Completed";

        public const string StageStatus_Active = "Active";
        public const string MembershipType_Owner = "Owner";

        public const string Search_Registration = "registration";
        public const string Autocomplete_TotalHits = "totalHits";
    }
}
