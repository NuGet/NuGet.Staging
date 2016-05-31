// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Search;

namespace NuGet.Services.Staging.Manager
{
    public class SearchServiceFactory : ISearchServiceFactory
    {
        private readonly StageContext _stageContext;
        private readonly IV3ServiceFactory _v3ServiceFactory;

        public SearchServiceFactory(StageContext stageContext, IV3ServiceFactory v3ServiceFactory)
        {
            if (stageContext == null)
            {
                throw new ArgumentNullException(nameof(stageContext));
            }

            if (v3ServiceFactory == null)
            {
                throw new ArgumentNullException(nameof(v3ServiceFactory));
            }

            _stageContext = stageContext;
            _v3ServiceFactory = v3ServiceFactory;
        }

        public ISearchService GetSearchService(string stageId)
        {
            return new DatabaseSearchService(_stageContext, _v3ServiceFactory.CreatePathCalculator(stageId), stageId);
        }
    }
}