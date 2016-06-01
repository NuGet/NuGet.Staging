// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NuGet.Indexing;
using NuGet.Services.Staging.Database.Models;
using NuGet.Versioning;
using Criteria = System.Func<string, System.Linq.Expressions.Expression<System.Func<NuGet.Services.Staging.Database.Models.PackageMetadata, bool>>>;

namespace NuGet.Services.Staging.Search
{
    public static class DatabaseSearchExpressionBuilder
    {
        private static readonly Criteria IdCriteria = term => p => p.Id.Contains(term);

        private static readonly Criteria DescriptionCriteria = term => p => p.Description.Contains(term);

        private static readonly Criteria SummaryCriteria = term => p => p.Summary.Contains(term);

        private static readonly Criteria TagCriteria = term => p => p.Tags.Contains(term);

        private static readonly Criteria AuthorCriteria = term => p => p.Authors.Contains(term);

        private static readonly Criteria VersionCriteria = term => p => string.Compare(p.Version, term, StringComparison.InvariantCultureIgnoreCase) == 0;

        private static readonly Criteria OwnerCriteria = term => p => p.Owners.Contains(term);

        private static readonly Criteria TitleCriteria = term => p => p.Title.Contains(term);

        private static readonly Criteria PackageIdCriteria = term => p => string.Compare(p.Id, term, StringComparison.InvariantCultureIgnoreCase) == 0;

        private static readonly Dictionary<QueryField, Criteria> QueryFieldToCriteriaMapping = new Dictionary<QueryField, Criteria>
            {
                { QueryField.Id, IdCriteria },
                { QueryField.PackageId, PackageIdCriteria },
                { QueryField.Version, VersionCriteria },
                { QueryField.Title, TitleCriteria },
                { QueryField.Description, DescriptionCriteria },
                { QueryField.Tag, TagCriteria },
                { QueryField.Author, AuthorCriteria },
                { QueryField.Summary, SummaryCriteria },
                { QueryField.Owner, OwnerCriteria },
            };

        public static Expression<Func<PackageMetadata, bool>> Build(string query)
        {
            // Parse the query string, so that a string like "TITLE:dot net ID:json" becomes
            // Grouping 1: Key=TITLE VALUE=[dot, net]
            // Grouping 2: Key=ID VALUE=[json] 
            var parser = new NuGetQueryParser();
            var groupings = parser.ParseQuery(query);

            // Empty query. Return everything
            if (!groupings.Any())
            {
                return p => true;
            }

            // Build a list of expressions for each term
            var expressions = new List<LambdaExpression>();

            foreach (var grouping in groupings)
            {
                Criteria fieldCriteria;
                if (QueryFieldToCriteriaMapping.TryGetValue(grouping.Key, out fieldCriteria))
                {
                    foreach (var value in grouping.Value)
                    {
                        string formatedValue;
                        if (!TryFormatValueForQuery(grouping.Key, value, out formatedValue))
                        {
                            // Illegal query. Result should be empty
                            return p => false;
                        }
                        expressions.Add(fieldCriteria(formatedValue));
                    }
                }
                else
                {
                    foreach (var criteria in QueryFieldToCriteriaMapping.Values)
                    {
                        expressions.AddRange(grouping.Value.Select(value => criteria(value)));
                    }
                }
            }

            // Build a giant or statement using the bodies of the lambdas
            var body = expressions.Select(p => p.Body).Aggregate(Expression.OrElse);

            // Now build the final predicate
            var parameterExpr = Expression.Parameter(typeof(PackageMetadata));

            // Fix up the body to use our parameter expression
            body = new ParameterExpressionReplacer(parameterExpr).Visit(body);

            // Build the final predicate
            var predicate = Expression.Lambda<Func<PackageMetadata, bool>>(body, parameterExpr);

            return predicate;
        }

        private static bool TryFormatValueForQuery(QueryField field, string value, out string formatedValue)
        {
            formatedValue = value;

            if (field == QueryField.Version)
            {
                NuGetVersion nuGetVersion;
                if (!NuGetVersion.TryParse(value, out nuGetVersion))
                {
                    return false;
                }

                formatedValue = nuGetVersion.ToNormalizedString();
            }

            return true;
        }

        private class ParameterExpressionReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameterExpr;

            public ParameterExpressionReplacer(ParameterExpression parameterExpr)
            {
                _parameterExpr = parameterExpr;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == _parameterExpr.Type &&
                    node != _parameterExpr)
                {
                    return _parameterExpr;
                }
                return base.VisitParameter(node);
            }
        }
    }
}
