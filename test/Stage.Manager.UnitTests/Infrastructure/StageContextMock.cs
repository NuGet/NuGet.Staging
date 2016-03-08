// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Entity;
using Moq;
using Stage.Database.Models;

namespace Stage.Manager.UnitTests
{
    public class StageContextMock : Mock<StageContext>
    {
        private readonly Dictionary<string, object> _tables;


        public StageContextMock()
        {
            _tables = new Dictionary<string, object>();
            MockTables();
        }

        public static DbSet<S> MockDbSet<S>(IEnumerable<S> table) where S : class
        {
            var mockDbSet = new Mock<DbSet<S>>();
            mockDbSet.As<IQueryable<S>>().Setup(q => q.Provider).Returns(() => table.AsQueryable().Provider);
            mockDbSet.As<IQueryable<S>>().Setup(q => q.Expression).Returns(() => table.AsQueryable().Expression);
            mockDbSet.As<IQueryable<S>>().Setup(q => q.ElementType).Returns(() => table.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<S>>().Setup(q => q.GetEnumerator()).Returns(() => table.AsQueryable().GetEnumerator());
            if (table is List<S>)
            {
                var list = (List<S>)table;
                mockDbSet.Setup(set => set.Add(It.IsAny<S>(), It.IsAny<GraphBehavior>()))
                    .Callback<S, GraphBehavior>((s, g) => list.Add(s));
                mockDbSet.Setup(set => set.AddRange(It.IsAny<IEnumerable<S>>(), It.IsAny<GraphBehavior>()))
                    .Callback<IEnumerable<S>, GraphBehavior>((s, g) => list.AddRange(s));
                mockDbSet.Setup(set => set.Remove(It.IsAny<S>())).Callback<S>(t => list.Remove(t));
                mockDbSet.Setup(set => set.RemoveRange(It.IsAny<IEnumerable<S>>())).Callback<IEnumerable<S>>(
                    ts =>
                    {
                        foreach (var t in ts)
                        {
                            list.Remove(t);
                        }
                    });
            }

            return mockDbSet.Object;
        }

        /// <summary>
        /// Mocks all the DbSet{T} properties that represent tables and views.
        /// </summary>
        private void MockTables()
        {
            Type contextType = typeof(StageContext);
            var dbSetProperties = contextType.GetProperties().Where(prop => (prop.PropertyType.IsGenericType) && prop.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
            foreach (var prop in dbSetProperties)
            {
                var dbSetGenericType = prop.PropertyType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(dbSetGenericType);
                var listForFakeTable = Activator.CreateInstance(listType);
                var parameter = Expression.Parameter(contextType);
                var body = Expression.PropertyOrField(parameter, prop.Name);
                var lambdaExpression = Expression.Lambda<Func<StageContext, object>>(body, parameter);
                var method = typeof(StageContextMock).GetMethod("MockDbSet").MakeGenericMethod(dbSetGenericType);
                Setup(lambdaExpression).Returns(method.Invoke(null, new[] { listForFakeTable }));
                _tables.Add(prop.Name, listForFakeTable);
            }
        }
    }
}
