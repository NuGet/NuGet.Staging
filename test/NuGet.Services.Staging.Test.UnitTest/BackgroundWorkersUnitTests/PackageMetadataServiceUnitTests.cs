// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.PackageService;
using NuGet.Services.Test.Common;
using Xunit;

namespace NuGet.Services.Staging.UnitTests.BackgroundWorkersUnitTests
{
    public class PackageMetadataServiceUnitTests
    {
        private readonly PackageMetadataService _packageMetadataService;
        private readonly Mock<IReadOnlyStorage> _readonlyStorageMock;

        public PackageMetadataServiceUnitTests()
        {
            _readonlyStorageMock = new Mock<IReadOnlyStorage>();
            _packageMetadataService = new PackageMetadataService(_readonlyStorageMock.Object);
        }

        [Fact]
        public async Task VerifyGetPackageDependencies()
        {
            // Arrange
            var dependencies = new[]
            {
                new PackageDependencyGroup(
                    new NuGetFramework("abc"),
                    new []
                    {
                        new PackageDependency("dependency1"),
                        new PackageDependency("dependency2"),
                    }),
                new PackageDependencyGroup(
                    new NuGetFramework("def"),
                    new []
                    {
                        new PackageDependency("dependency1"),
                        new PackageDependency("dependency3"),
                    }),
            };

            var nuspecUri = new Uri("http://someuri");

            var testPackage = new TestPackage("package", "1.0.0").WithDependencies(dependencies);
            _readonlyStorageMock.Setup(x => x.ReadAsString(nuspecUri)).Returns(Task.FromResult(testPackage.Nuspec));

            // Act
            var packageDependencies = (await _packageMetadataService.GetPackageDependencies(new PackagePushData
            {
                NuspecPath = nuspecUri.ToString()
            })).ToList();

            // Assert
            packageDependencies.Should().HaveCount(3);
            packageDependencies.Should().Contain(x => x.Id == "dependency1");
            packageDependencies.Should().Contain(x => x.Id == "dependency2");
            packageDependencies.Should().Contain(x => x.Id == "dependency3");
        }
    }
}
