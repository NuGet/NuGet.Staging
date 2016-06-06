// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.V3Repository;
using NuGet.Versioning;

namespace NuGet.Services.Staging.Search
{
    public class DatabaseSearchResultsFormatter
    {
        private readonly V3PathGenerator _pathGenerator;

        public DatabaseSearchResultsFormatter(V3PathGenerator pathGenerator)
        {
            if (pathGenerator == null)
            {
                throw new ArgumentNullException(nameof(pathGenerator));
            }

            _pathGenerator = pathGenerator;
        }

        public JObject FormatResults(IEnumerable<PackageMetadata> packagesMetadata)
        {
            // If there are several packages with the same Id, but different version,
            // we would return one result per id
            var idGroups = packagesMetadata.GroupBy(x => x.Id);
            var formatedPackages = idGroups.Select(FormatPackageResult);

            return new JObject
            {
                {
                    "@context", new JObject
                    {
                        {
                            "@vocab", "http://schema.nuget.org/schema#"
                        }
                    }
                },
                {
                    "data", new JArray(formatedPackages)
                }
            };
        }

        private JObject FormatPackageResult(IEnumerable<PackageMetadata> packagesMetadata)
        {
            // Sort packages from 
            var sortedPackages = packagesMetadata.OrderBy(x => x, new PackageMetadataVersionComparer());
            var newestVersion = sortedPackages.Last();

            var authorsList = newestVersion.Authors.Split(',').Select(s => s.Trim());
            var tagsList = newestVersion.Tags.Split(' ');

            var versionsList = sortedPackages.Select(FormatVersionResult);

            return new JObject
            {
                { "@id", _pathGenerator.GetPackageRegistrationIndexAddress(newestVersion.Id).ToString() },
                { "@type", "Package" },
                { "authors", new JArray(authorsList) },
                { "description",newestVersion.Description },
                { "iconUrl", newestVersion.IconUrl },
                { "id", newestVersion.Id},
                { "licenseUrl", newestVersion.LicenseUrl },
                { "projectUrl", newestVersion.ProjectUrl },
                { "registration", _pathGenerator.GetPackageRegistrationIndexAddress(newestVersion.Id).ToString() },
                { "tags", new JArray(tagsList) },
                { "title", newestVersion.Title },
                { "totalDownloads", 0 },
                { "version", newestVersion.Version },
                { "versions", new JArray(versionsList) }
            };
        }

        private JObject FormatVersionResult(PackageMetadata packageMetadata)
        {
            return new JObject
            {
                { "@id", _pathGenerator.GetPackageVersionRegistrationAddress(packageMetadata.Id, packageMetadata.Version).ToString() },
                { "downloads", 0 },
                { "version", packageMetadata.Version }
            };
        }

        private class PackageMetadataVersionComparer : IComparer<PackageMetadata>
        {
            public int Compare(PackageMetadata x, PackageMetadata y)
            {
                var xVersion = new NuGetVersion(x.Version);
                var yVersion = new NuGetVersion(y.Version);

                return xVersion.CompareTo(yVersion);
            }
        }
    }
}
