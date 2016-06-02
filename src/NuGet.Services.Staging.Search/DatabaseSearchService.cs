// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public DatabaseSearchService(StageContext stageContext, V3PathGenerator pathGenerator, string stageId)
        {
            if (stageContext == null)
            {
                throw new ArgumentNullException(nameof(stageContext));
            }

            if (pathGenerator == null)
            {
                throw new ArgumentNullException(nameof(pathGenerator));
            }

            _context = stageContext;
            _formatter = new DatabaseSearchResultsFormatter(pathGenerator);
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
            var queryDictionary = 
                QueryHelpers.ParseNullableQuery(query)?.ToDictionary(x => x.Key, x => (string)x.Value)
                ?? new Dictionary<string, string>();

            var skip = GetSkip(queryDictionary);
            var take = GetTake(queryDictionary);
            var includePrerelease = GetIncludePrerelease(queryDictionary);
            var q = GetQuery(queryDictionary);

            return _formatter.FormatSearchResults(ApplyQueryParameters(stage.Key, includePrerelease, q, skip, take));
        }

        internal IReadOnlyList<PackageMetadata> ApplyQueryParameters(int stageKey, bool includePrerelease, string query, int skip, int take)
        {
            // Why do we need to get a list of ids and only after that to get packages? In case there are multiple versions
            // of the same package, all the versions will appear in the results, making take and skip give the wrong results.
            // So first get the distinct list of ids that should be in the results, and only after translate them to the list of packages 
            // with all the versions.

            var idsResult = ApplyStageFilter(stageKey);
            idsResult = ApplyIncludePrerelease(idsResult, includePrerelease);
            idsResult = ApplyQueryFilter(idsResult, query);

            var distinctIds = idsResult.Select(p => p.Id).Distinct().OrderBy(id => id);

            var ids = distinctIds.Skip(skip).Take(take);
            var idsList = ids.ToList();

            var searchResults = ApplyStageFilter(stageKey);
            searchResults = ApplyIncludePrerelease(searchResults, includePrerelease);

            return searchResults.Where(x => idsList.Contains(x.Id)).OrderBy(x => x.Id).ToImmutableList();
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

        private static string GetQuery(Dictionary<string, string> queryDictionary)
        {
            string query;

            if (!queryDictionary.TryGetValue("q", out query))
            {
                query = string.Empty;
            }

            return query;
        }
    }
}