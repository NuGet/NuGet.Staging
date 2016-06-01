// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NuGet.Packaging;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager
{
    public static class ExtensionMethods
    {
        public static PackageMetadata LoadFromNuspec(this PackageMetadata packageMetadata, NuspecReader nuspecReader, UserInformation userInformation)
        {
            var metadataDictionary = nuspecReader.GetMetadata().ToImmutableDictionary();
            var version = nuspecReader.GetVersion();

            packageMetadata.Id = nuspecReader.GetId();
            packageMetadata.Version = version.ToNormalizedString();
            packageMetadata.Authors = FlattenString(GetValue(metadataDictionary, "authors"));
            packageMetadata.Description = GetValue(metadataDictionary, "description");
            packageMetadata.IconUrl = GetValue(metadataDictionary, "iconUrl");
            packageMetadata.LicenseUrl = GetValue(metadataDictionary, "licenseUrl");
            packageMetadata.ProjectUrl = GetValue(metadataDictionary, "projectUrl");
            packageMetadata.Tags = ParseTags(GetValue(metadataDictionary, "tags"));
            packageMetadata.Title = GetValue(metadataDictionary, "title");
            packageMetadata.Summary = GetValue(metadataDictionary, "summary");
            packageMetadata.Owners = userInformation.UserName;
            packageMetadata.IsPrerelease = version.IsPrerelease;

            return packageMetadata;
        }

        private static string GetValue(IDictionary<string, string> dictionary, string key)
        {
            var value = string.Empty;
            dictionary.TryGetValue(key, out value);

            return value;
        }

        private static string FlattenString(string commaSeparatedList)
        {
            return string.Join(", ", commaSeparatedList.Split(',').Select(x => x.Trim()));
        }

        private static string ParseTags(string tags)
        {
            if (tags == null)
            {
                return null;
            }

            tags = tags.Replace(',', ' ').Replace(';', ' ').Replace('\t', ' ').Replace("  ", " ");
            var tagsList = tags.Split(' ').Where(x => !string.IsNullOrEmpty(x));

            return string.Join(" ", tagsList);
        }
    }
}
