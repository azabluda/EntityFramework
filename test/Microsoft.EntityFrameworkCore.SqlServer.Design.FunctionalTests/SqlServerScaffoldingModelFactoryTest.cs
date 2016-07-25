// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.FunctionalTests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.EntityFrameworkCore.SqlServer.Design.FunctionalTests
{
    public class SqlServerScaffoldingModelFactoryTest : IDisposable
    {
        private readonly IScaffoldingModelFactory _scaffoldingModelFactory;
        private readonly DbContext _context;
        private readonly SqlServerTestStore _testStore;

        public SqlServerScaffoldingModelFactoryTest()
        {
            _testStore = SqlServerTestStore.CreateScratch();

            var serviceProvider = new SqlServerDesignTimeServices()
                .ConfigureDesignTimeServices(
                    new ServiceCollection().AddScaffolding().AddLogging())
                .BuildServiceProvider();

            _scaffoldingModelFactory = serviceProvider
                .GetService<IScaffoldingModelFactory>();

            _context = new DbContext(
                new DbContextOptionsBuilder()
                    .UseSqlServer(_testStore.Connection)
                    .Options);
        }

        [Fact]
        public void It_gets_unique_indexes_on_nullable_columns()
        {
            var sql = @"CREATE TABLE [dbo].[Place] (
    [Id] int NOT NULL IDENTITY,
    [Name] int,
    CONSTRAINT [PK_Place] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Place_Name] ON [dbo].[Place] ([Name]) WHERE [Name] IS NOT NULL;
";
            var model = GetModel(sql);
            var script = GenerateScript(model);
            Assert.Equal(sql, script);
        }

        private IModel GetModel(string createSql)
        {
            _testStore.ExecuteNonQuery(createSql);
            return _scaffoldingModelFactory.Create(_testStore.ConnectionString, TableSelectionSet.All);
        }

        private string GenerateScript(IModel model)
        {
            var operations = _context
                .GetService<IMigrationsModelDiffer>()
                .GetDifferences(null, model);

            var commandList = _context
                .GetService<IMigrationsSqlGenerator>()
                .Generate(operations);

            return string.Join(Environment.NewLine, commandList.Select(c => c.CommandText));
        }

        public void Dispose()
        {
            _context.Dispose();
            _testStore.Dispose();
        }
    }
}
