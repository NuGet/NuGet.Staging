// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.V3Repository;

namespace NuGet.Services.Staging.Search
{
    public class DatabaseSearchService : ISearchService
    {
        private readonly StageContext _context;
        private readonly DatabaseSearchResultsFormatter _formatter;
        private readonly string _stageId;

        public DatabaseSearchService(StageContext stageContext, V3PathCalculator pathCalculator, string stageId)
        {
            if (stageContext == null)
            {
                throw new ArgumentNullException(nameof(stageContext));
            }

            if (pathCalculator == null)
            {
                throw new ArgumentNullException(nameof(pathCalculator));
            }

            _context = stageContext;
            _formatter = new DatabaseSearchResultsFormatter(pathCalculator);
            _stageId = stageId;
        }

        public JObject Search(string query)
        {
            var stage = _context.Stages.FirstOrDefault(x => x.Id == _stageId);

            if (stage == null)
            {
                return null;
            }

            query = query.ToLowerInvariant();
            var queryDictionary = QueryHelpers.ParseNullableQuery(query).ToDictionary(x => x.Key, x => (string)x.Value);

            var skip = GetSkip(queryDictionary);
            var take = GetTake(queryDictionary);
            var includePrerelease = GetIncludePrerelease(queryDictionary);
            var q = queryDictionary["q"];

            return _formatter.FormatSearchResults(ApplyQueryParameters(stage.Key, includePrerelease, q, skip, take));
        }

        internal IEnumerable<PackageMetadata> ApplyQueryParameters(int stageKey, bool includePrerelease, string query, int skip, int take)
        {
            var searchResult = ApplyStageFilter(stageKey);
            searchResult = ApplyIncludePrerelease(searchResult, includePrerelease);
            searchResult = ApplyQueryFilter(searchResult, query);
            searchResult = searchResult.Skip(skip);
            searchResult = searchResult.Take(take);

            return searchResult;
        }

        private IQueryable<PackageMetadata> ApplyStageFilter(int stageKey)
        {
            return _context.PackagesMetadata.Where(pm => pm.StageKey == stageKey);
        }

        private static IQueryable<PackageMetadata> ApplyIncludePrerelease(IQueryable<PackageMetadata> packages, bool includePrerelease)
        {
            return includePrerelease
                ? packages
                : packages.Where(pm => !pm.IsPrerelease);
        }

        private IQueryable<PackageMetadata> ApplyQueryFilter(IQueryable<PackageMetadata> packages, string query)
        {
            var predicate = DatabaseSearchExpressionBuilder.Build(query);
            return packages.Where(predicate);
        }
        
        private static bool GetIncludePrerelease(Dictionary<string, string> queryDictionary)
        {
            string includePrerelease;

            if (queryDictionary.TryGetValue("prerelease", out includePrerelease))
            {
                bool includePrereleaseBool;
                if (bool.TryParse(includePrerelease, out includePrereleaseBool))
                {
                    return includePrereleaseBool;
                }
            }

            return false;
        }
       
        private static int GetTake(Dictionary<string, string> queryDictionary)
        {
            string take;

            if (queryDictionary.TryGetValue("take", out take))
            {
                int takeInt;
                if (int.TryParse(take, out takeInt) && takeInt > 1 && takeInt < 1000)
                {
                    return takeInt;
                }
            }

            return 20;
        }

        private static int GetSkip(Dictionary<string, string> queryDictionary)
        {
            string skip;

            if (queryDictionary.TryGetValue("skip", out skip))
            {
                int skipInt;
                if (int.TryParse(skip, out skipInt) && skipInt > 0)
                {
                    return skipInt;
                }
            }

            return 0;
        }
    }
}