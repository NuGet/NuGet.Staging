// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using NuGet.Services.Metadata.Catalog;
using NuGet.Services.Metadata.Catalog.Dnx;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.Registration;
using Stage.V3;
using VDS.RDF;

namespace NuGet.V3Repository
{
    public class V3ServiceOptions
    {
        public string FlatContainerFolderName { get; set; }

        public string RegistrationFolderName { get; set; }

        public string CatalogFolderName { get; set; }
    }

    internal class V3PackageMetadata : IPackageMetadata
    {
        public V3PackageMetadata(NupkgMetadata metadata)
        {
            NupkgMetadata = metadata;
        }

        public XDocument Nuspec => NupkgMetadata.Nuspec;

        public NupkgMetadata NupkgMetadata { get; set; }
    }

    public class V3Service : IV3Service
    {
        private readonly V3ServiceOptions _options;
        private readonly DnxMaker _dnxMaker;
        private readonly ILogger<V3Service> _logger;
        private readonly Storage _catalogStorage;
        private readonly StorageFactory _registrationStorageFactory;
        private readonly StorageFactory _flatContainerStorageFactory;

        private static readonly DateTime _dateTimeMinValueUtc = new DateTime(0L, DateTimeKind.Utc);

        public V3Service(V3ServiceOptions options, StorageFactory storageFactory, ILogger<V3Service> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            if (storageFactory == null)
            {
                throw new ArgumentNullException(nameof(storageFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _options = options;

            // Flat container 
            _flatContainerStorageFactory = new AppendingStorageFactory(storageFactory, _options.FlatContainerFolderName);
            _dnxMaker = new DnxMaker(_flatContainerStorageFactory);
            _logger = logger;

            // Catalog
            var catalogStorageFactory = new AppendingStorageFactory(storageFactory, _options.CatalogFolderName);
            _catalogStorage = catalogStorageFactory.Create();

            // Registration
            _registrationStorageFactory = new AppendingStorageFactory(storageFactory, _options.RegistrationFolderName);
        }

        public IPackageMetadata ParsePackageStream(Stream stream)
        {
            var nupkgMetadata = Utils.GetNupkgMetadata(stream);
            return new V3PackageMetadata(nupkgMetadata);
        }

        public async Task<Uri> AddPackage(Stream stream, IPackageMetadata packageMetadata)
        {
            // TODO: need to lock the stage before applying changes

            var v3PackageMetadata = (V3PackageMetadata) packageMetadata;
            var nuspec = new NuspecReader(v3PackageMetadata.Nuspec);

            string id = nuspec.GetId();
            string version = nuspec.GetVersion().ToNormalizedString();

            // TODO: consider closing the steam after save to flat container is done
            var packageLocations = await AddToFlatContainer(stream, packageMetadata, id, version);
            
            Tuple<Uri, IGraph> catalogItem = await AddToCatalog(v3PackageMetadata, id, version);

            RegistrationMakerCatalogItem.PackagePathProvider = new FlatContainerPathProvider(packageLocations.Nupkg.ToString());
            await RegistrationMaker.Process(
                new RegistrationKey(id),
                new Dictionary<string, IGraph> {{ catalogItem.Item1.ToString(), catalogItem.Item2 }},
                _registrationStorageFactory,
                _flatContainerStorageFactory.BaseAddress,
                64,
                128,
                cancellationToken: CancellationToken.None);

            return packageLocations.Nupkg;
        }

        private async Task<DnxMaker.DnxEntry> AddToFlatContainer(Stream stream, IPackageMetadata packageMetadata, string id, string version)
        {
            _logger.LogInformation($"Adding package: {id}, {version}");

            stream.Position = 0;
            var packageLocations =
                await _dnxMaker.AddPackage(stream, packageMetadata.Nuspec.ToString(), id, version, CancellationToken.None);

            _logger.LogInformation($"Package {id}, {version} was added to flat container");

            return packageLocations;
        }

        private async Task<Tuple<Uri, IGraph>> AddToCatalog(V3PackageMetadata packageMetadata, string id, string version)
        {
            DateTime timestamp = DateTime.UtcNow;
            DateTime lastDeleted = _dateTimeMinValueUtc;

            string catalogIndex = await LoadCatalog(_catalogStorage);

            if (catalogIndex != null)
            {
                lastDeleted = GetCatalogProperty(catalogIndex, "nuget:lastDeleted") ?? _dateTimeMinValueUtc;
            }

            var writer = new AppendOnlyCatalogWriter(_catalogStorage, maxPageSize: 550);
            var catalogItem = new PackageCatalogItem(packageMetadata.NupkgMetadata, timestamp);

            writer.Add(catalogItem);

            var commitMetadata = PackageCatalog.CreateCommitMetadata(writer.RootUri, new CommitMetadata(timestamp, timestamp, lastDeleted));

            var savedItems = await writer.Commit(commitMetadata, CancellationToken.None);

            _logger.LogInformation($"Package {id}, {version} was added to catalog");

            return new Tuple<Uri, IGraph>(savedItems.First(), catalogItem.CreateContentGraph(new CatalogContext()));
        }


        private async Task<string> LoadCatalog(Storage storage)
        {
            return await storage.LoadString(storage.ResolveUri("index.json"), CancellationToken.None);
        }

        private DateTime? GetCatalogProperty(string index, string propertyName)
        {
            var obj = JObject.Parse(index);

            JToken token;
            if (obj.TryGetValue(propertyName, out token))
            {
                return token.ToObject<DateTime>().ToUniversalTime();
            }

            return null;
        }

        class FlatContainerPathProvider : IPackagePathProvider
        {
            private string _path;

            public FlatContainerPathProvider(string path)
            {
                _path = path;
            }

            public string GetPackagePath(string id, string version)
            {
                return _path;
            }
        }
    }
}
 