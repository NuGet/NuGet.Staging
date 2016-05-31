// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Staging.Search;

namespace NuGet.Services.Staging.Manager
{
    public interface ISearchServiceFactory
    {
        ISearchService GetSearchService(string stageId);
    }
}
