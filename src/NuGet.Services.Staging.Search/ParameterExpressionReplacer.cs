// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace NuGet.Services.Staging.Search
{
    internal class ParameterExpressionReplacer : ExpressionVisitor
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