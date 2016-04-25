// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.V3Repository;
using Xunit;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    [Collection("Packages test collection")]
    public class V3ServiceUnitTests
    {
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
            _v3Service = new V3Service(_options, _storageFactory, new Mock<ILogger<V3Service>>().Object);
        }

        [Fact]
        public async Task WhenAddPackageIsCalledPackageIsAddedToFlatContainer()
        {
            // Arrange 
            var testPackage = new TestPackage("DefaultId", "1.0.0").WithDefaultData();

            // Act
            var metadata = _v3Service.ParsePackageStream(testPackage.Stream);
            var packageUris = await _v3Service.AddPackage(testPackage.Stream, metadata);

            // Assert
            string flatContainerPath = $"{_options.FlatContainerFolderName}/{testPackage.Id}";
            _storageFactory.CreatedStorages.ContainsKey(flatContainerPath).Should().BeTrue("correct flat-container path");

            var flatContainerStorage = _storageFactory.CreatedStorages[flatContainerPath] as MemoryStorage;
            flatContainerStorage.Content.Count.Should().BeGreaterThan(0, "flat container shouldn't be empty");

            ((MemoryStorage) _storageFactory.CreatedStorages[flatContainerPath]).Content.ContainsKey(packageUris.Nupkg)
                .Should()
                .BeTrue("Returned value should be nupkg location");
            ((MemoryStorage)_storageFactory.CreatedStorages[flatContainerPath]).Content.ContainsKey(packageUris.Nuspec)
                .Should()
                .BeTrue("Returned value should be nupkg location");
        }

        [Fact]
        public async Task WhenAddPackageIsCalledForTheFirstTimeCatalogBuilt()
        {
            // Arrange 
            var testPackage = new TestPackage("defaultid", "1.0.0").WithDefaultData();

            // Act
            var metadata = _v3Service.ParsePackageStream(testPackage.Stream);
            await _v3Service.AddPackage(testPackage.Stream, metadata);

            // Assert
            string catalogPath = $"{_options.CatalogFolderName}/";
            _storageFactory.CreatedStorages.ContainsKey(catalogPath).Should().BeTrue("Catalog was created");

            var catalogStorage = (MemoryStorage)_storageFactory.CreatedStorages[catalogPath];
            catalogStorage.Content.ContainsKey(new Uri($"{catalogStorage.BaseAddress}index.json")).Should().BeTrue("index file should exist");
            catalogStorage.Content.ContainsKey(new Uri($"{catalogStorage.BaseAddress}page0.json")).Should().BeTrue("page file should exist");
            catalogStorage.Content.Any(x => x.Key.ToString().Contains($"{testPackage.Id}.{testPackage.Version}")).Should().BeTrue("Data file exists");
        }

        [Fact]
        public async Task WhenAddPackageIsCalledRegistrationIsBuilt()
        {
            // Arrange 
            var testPackage = new TestPackage("defaultid", "1.0.0").WithDefaultData();

            // Act
            var metadata = _v3Service.ParsePackageStream(testPackage.Stream);
            var packageUris = await _v3Service.AddPackage(testPackage.Stream, metadata);

            // Assert
            string registrationPath = $"{_options.RegistrationFolderName}/{testPackage.Id}";
            _storageFactory.CreatedStorages.ContainsKey(registrationPath).Should().BeTrue("Registration was created");

            var registrationStorage = (MemoryStorage) _storageFactory.CreatedStorages[registrationPath];
            StorageContent indexFile, versionFile;

            registrationStorage.Content.TryGetValue(new Uri($"{registrationStorage.BaseAddress}index.json"), out indexFile).Should().BeTrue("index file should exist");
            registrationStorage.Content.TryGetValue(new Uri($"{registrationStorage.BaseAddress}{testPackage.Version}.json"), out versionFile).Should().BeTrue("version file should exist");
            
            // Verify index file
            var indexFileObj = ParseStorageContent(indexFile);

            var catalogDataPath =
                ((MemoryStorage) _storageFactory.CreatedStorages[$"{_options.CatalogFolderName}/"]).Content.First(
                    x => x.Key.ToString().Contains($"{testPackage.Id}.{testPackage.Version}")).Key;

            var id = indexFileObj["items"].First()["items"].First()["catalogEntry"]["@id"].ToString();
            id.Should().Be(catalogDataPath.ToString(), "Catalog path in index should file be correct");

            var packageContent = indexFileObj["items"].First()["items"].First()["catalogEntry"]["packageContent"].ToString();
            packageContent.Should().Be(packageUris.Nupkg.ToString(), "Package content path in catalogEntry index file should be correct");

            var packageContent2 = indexFileObj["items"].First()["items"].First()["packageContent"].ToString();
            packageContent2.Should().Be(packageUris.Nupkg.ToString(), "Package content path in index should be correct");

            // Verify version file
            var versionFileObj = ParseStorageContent(versionFile);
            var catalogEntry = versionFileObj["catalogEntry"].ToString();
            catalogEntry.Should().Be(catalogDataPath.ToString(), "Catalog path in version file should be correct");

            var packageContent3 = versionFileObj["packageContent"].ToString();
            packageContent3.Should().Be(packageUris.Nupkg.ToString(), "Package content path in version file should be correct");
        }

        private static JObject ParseStorageContent(StorageContent storageContent)
        {
            StreamReader sr = new StreamReader(storageContent.GetContentStream());
            string str = sr.ReadToEnd();

            var obj = JObject.Parse(str);
            return obj;
        }
    }
}
