// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NuGet.Packaging;

namespace NuGet.Services.Test.Common
{
    public class TestPackage
    {
        public string Id { get; }
        public string Version { get; }

        public Stream Stream { get; private set; }

        public string Nuspec { get; private set; }

        public const string DefaultTitle = "Test Package";
        public const string DefaultSummary = "This package is for testing NuGet";
        public const string DefaultAuthors = "nuget";
        public const string DefaultOwners = "Package owners";
        public const string DefaultDescription = "This package is for testing NuGet";
        public const string DefaultTags = "nuget test";
        public const string DefaultIconUrl = "http://myicon";
        public const string DefaultLicenseUrl = "http://license";
        public const string DefaultProjectUrl = "http://projecturl";

        public TestPackage(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public TestPackage WithDefaultData()
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
                var nuspecEntry = packageArchive.CreateEntry(Id + ".nuspec", CompressionLevel.Fastest);
                using (var stream = nuspecEntry.Open())
                {
                    WriteNuspec(stream, true, Id, Version);
                }
            });

            return this;
        }

        public TestPackage WithInvalidNuspec()
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
            });

            Nuspec = string.Empty;

            return this;
        }

        public TestPackage WithMinClientVersion(string minClientVersion)
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
                var nuspecEntry = packageArchive.CreateEntry(Id + ".nuspec", CompressionLevel.Fastest);
                using (var stream = nuspecEntry.Open())
                {
                    WriteNuspec(stream, true, Id, Version, minClientVersion);
                }
            });

            return this;
        }

        public TestPackage WithDependencies(IEnumerable<PackageDependencyGroup> packageDependencyGroups)
        {
            Stream = CreateTestPackageStream(packageArchive =>
            {
                var nuspecEntry = packageArchive.CreateEntry(Id + ".nuspec", CompressionLevel.Fastest);
                using (var stream = nuspecEntry.Open())
                {
                    WriteNuspec(stream, true, Id, Version, null, packageDependencyGroups);
                }
            });

            return this;
        }

        private static Stream CreateTestPackageStream(Action<ZipArchive> populatePackage)
        {
            var packageStream = new MemoryStream();
            using (var packageArchive = new ZipArchive(packageStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                populatePackage?.Invoke(packageArchive);
            }

            packageStream.Position = 0;

            return packageStream;
        }

        private void WriteNuspec(
            Stream stream,
            bool leaveStreamOpen,
            string id,
            string version,
            string minClientVersion = null,
            IEnumerable<PackageDependencyGroup> packageDependencyGroups = null,
            string title = DefaultTitle,
            string summary = DefaultSummary,
            string authors = DefaultAuthors,
            string owners = DefaultOwners,
            string description = DefaultDescription,
            string tags = DefaultTags,
            string language = null,
            string copyright = null,
            string releaseNotes = null,
            string licenseUrl = DefaultLicenseUrl,
            string projectUrl = DefaultProjectUrl,
            string iconUrl = DefaultIconUrl,
            bool requireLicenseAcceptance = false)
        {
            using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(false, true), 1024, leaveStreamOpen))
            {
                Nuspec = @"<?xml version=""1.0""?>
                    <package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
                        <metadata" +
                         (!string.IsNullOrEmpty(minClientVersion)
                             ? @" minClientVersion=""" + minClientVersion + @""""
                             : string.Empty) + @">
                            <id>" + id + @"</id>
                            <version>" + version + @"</version>
                            <title>" + title + @"</title>
                            <summary>" + summary + @"</summary>
                            <description>" + description + @"</description>
                            <tags>" + tags + @"</tags>
                            <requireLicenseAcceptance>" + requireLicenseAcceptance + @"</requireLicenseAcceptance>
                            <authors>" + authors + @"</authors>
                            <owners>" + owners + @"</owners>
                            <language>" + (language ?? string.Empty) + @"</language>
                            <copyright>" + (copyright ?? string.Empty) + @"</copyright>
                            <releaseNotes>" + (releaseNotes ?? string.Empty) + @"</releaseNotes>
                            <licenseUrl>" + licenseUrl + @"</licenseUrl>
                            <projectUrl>" + projectUrl + @"</projectUrl>
                            <iconUrl>" + iconUrl + @"</iconUrl>
                            <dependencies>" + WriteDependencies(packageDependencyGroups) + @"</dependencies>
                        </metadata>
                    </package>";
                streamWriter.WriteLine(Nuspec);
            }
        }

        private static string WriteDependencies(IEnumerable<PackageDependencyGroup> packageDependencyGroups)
        {
            if (packageDependencyGroups == null || !packageDependencyGroups.Any())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var packageDependencyGroup in packageDependencyGroups)
            {
                builder.Append("<group");
                if (packageDependencyGroup.TargetFramework != null)
                {
                    builder.AppendFormat(" targetFramework=\"{0}\"", packageDependencyGroup.TargetFramework.GetShortFolderName());
                }
                builder.Append(">");

                foreach (var packageDependency in packageDependencyGroup.Packages)
                {
                    builder.AppendFormat("<dependency id=\"{0}\"", packageDependency.Id);
                    if (packageDependency.VersionRange != null)
                    {
                        builder.AppendFormat(" version=\"{0}\"", packageDependency.VersionRange);
                    }
                    builder.Append(">");
                    builder.Append("</dependency>");
                }

                builder.Append("</group>");
            }

            return builder.ToString();
        }
    }
}
