// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Stage.Manager.Controllers
{
    public static class Messages
    {
        public const string InvalidStageIdMessage = 
            @"Provide a valid stage id Guid";

        public const string InvalidStageDisplayName =
            @"Provide a non-empty display name with length up to {0} characters";

        public const string NuspecErrorMessage =
            @"The NuGet package contains an invaid .nuspec file. The error encountered was:'{0}'. Correct the error and try again.";

        public const string MinClientVersionOutOfRangeMessage =
            @"This package requires version '{0}' of NuGet, which this gallery does not currently support. Please contact us if you have questions.";

        public const string PackageExistsOnStageMessage =
            @"A package with id '{0}' and version '{1}' already exists on stage '{2}'. Delete and add to modify.";

        public const string ApiKeyUnauthorizedMessage =
            @"The specified API key is invalid or does not have permission to access the specified package.";

        public const string PackageAlreadyExists =
            @"A package with id '{0}' and version '{1} already exists on NuGet.org. Commit of this stage will fail.";
    }
}
