// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.V3Repository;
using Xunit;

namespace Stage.Manager.UnitTests
{
    public class V3ServiceUnitTests
    {
        // creates structure successfuly 

        private const string BaseAddress = "http://nuget.org/";

        private V3Service _v3Service;
        private V3ServiceOptions _options;
        private TestStorageFactory _storageFactory;

        public V3ServiceUnitTests()
        { 
            // Arrange
            _options = new V3ServiceOptions
            {
                CatalogFolderName = "catalog",
                FlatContainerFolderName = "flatcontainer",
                RegistrationFolderName = "registration",
            };

            _storageFactory = new TestStorageFactory((string s) => new MemoryStorage(new Uri(BaseAddress + s)));
            _v3Service = new V3Service(_options, _storageFactory);
        }

        [Fact]
        public async Task WhenAddPackageIsCalledPackageIsAdded()
        {
            // Arrange 
            const string RegistrationId = "DefaultId";
            var testPackage = new TestPackage(RegistrationId, "1.0.0").WithDefaultData();

            // Act
            await _v3Service.AddPackage(testPackage.Stream, testPackage.Nuspec, testPackage.Id, testPackage.Version);

            // Assert
            string flatContainerPath = $"{_options.FlatContainerFolderName}/{RegistrationId}";
            _storageFactory.CreatedStorages.ContainsKey(flatContainerPath).Should().BeTrue("correct flat-container path");

            var flatContainerStorage = _storageFactory.CreatedStorages[flatContainerPath] as MemoryStorage;
            flatContainerStorage.Content.Count.Should().BeGreaterThan(0, "flat container shouldn't be empty");
        }
    }
}
