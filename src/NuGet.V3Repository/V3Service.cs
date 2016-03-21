// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using NuGet.Services.Metadata.Catalog;
using NuGet.Services.Metadata.Catalog.Dnx;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.V3;

namespace NuGet.V3Repository
{
    public class V3ServiceOptions
    {
        public string FlatContainerFolderName { get; set; }

        public string RegistrationFolderName { get; set; }

        public string CatalogFolderName { get; set; }
    }

    public class V3PackageMetadata : IPackageMetadata
    {
        public V3PackageMetadata(NupkgMetadata metadata)
        {
            NupkgMetadata = metadata;
        }

        public XDocument Nuspec => NupkgMetadata.Nuspec;
        
        internal NupkgMetadata NupkgMetadata { get; set; }
    }

    public class V3Service : IV3Service
    {
        private readonly V3ServiceOptions _options;
        private readonly DnxMaker _dnxMaker;
        private readonly ILogger<V3Service> _logger;
        private readonly Storage _catalogStorage;

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
            _dnxMaker = new DnxMaker(new AppendingStorageFactory(storageFactory, _options.FlatContainerFolderName));
            _logger = logger;

            // Catalog
            var catalogStorageFactory = new AppendingStorageFactory(storageFactory, _options.CatalogFolderName);
            _catalogStorage = catalogStorageFactory.Create();
        }

        public IPackageMetadata ParsePackageStream(Stream stream)
        {
            var nupkgMetadata = GetNupkgMetadata(stream);

            return new V3PackageMetadata(nupkgMetadata);
        }

        public async Task<Uri> AddPackage(Stream stream, IPackageMetadata packageMetadata)
        {
            V3PackageMetadata v3PackageMetadata = (V3PackageMetadata) packageMetadata;
            var nuspec = new NuspecReader(v3PackageMetadata.Nuspec);

            string id = nuspec.GetId();
            string version = nuspec.GetVersion().ToNormalizedString();

            _logger.LogInformation($"Adding package: {id}, {version}");

            Tuple<Uri, Uri> packageLocations = await _dnxMaker.AddPackage(stream, packageMetadata.Nuspec.ToString(), id, version, CancellationToken.None);

            _logger.LogInformation($"Package {id}, {version} was added to flat container");

            await AddToCatalog(v3PackageMetadata);

            _logger.LogInformation($"Package {id}, {version} was added to catalog");

            return packageLocations.Item2;
        }

        private async Task AddToCatalog(V3PackageMetadata packageMetadata)
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

            await writer.Commit(commitMetadata, CancellationToken.None);
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
        
        /// Copies
        
        public static NupkgMetadata GetNupkgMetadata(Stream stream)
        {
            var nupkgMetadata = new NupkgMetadata
            {
                PackageSize = stream.Length,
                PackageHash = Utils.GenerateHash(stream)
            };

            stream.Seek(0, SeekOrigin.Begin);

            using (ZipArchive package = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                nupkgMetadata.Nuspec = Utils.GetNuspec(package);

                if (nupkgMetadata.Nuspec == null)
                {
                    throw new InvalidDataException("Unable to find nuspec");
                }

                nupkgMetadata.Entries = GetEntries(package);

                return nupkgMetadata;
            }
        }

        public static IEnumerable<PackageEntry> GetEntries(ZipArchive package)
        {
            IList<PackageEntry> result = new List<PackageEntry>();

            foreach (ZipArchiveEntry entry in package.Entries)
            {
                if (entry.FullName.EndsWith("/.rels", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.FullName.EndsWith("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.FullName.EndsWith(".psmdcp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new PackageEntry(entry));
            }

            return result;
        }
    }
}
 