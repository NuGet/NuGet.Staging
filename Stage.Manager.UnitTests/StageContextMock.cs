// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Moq;
using Stage.Database.Models;

namespace Stage.Manager.UnitTests
{
    public class StageContextMock : Mock<StageContext>
    {
        private Dictionary<string, object> tables;


        public StageContextMock()
        {
            MockTables();
        }

        // <summary>
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
                this.Setup(lambdaExpression).Returns(method.Invoke(null, new[] { listForFakeTable }));
                this.tables.Add(prop.Name, listForFakeTable);
            }
        }
    }
}
