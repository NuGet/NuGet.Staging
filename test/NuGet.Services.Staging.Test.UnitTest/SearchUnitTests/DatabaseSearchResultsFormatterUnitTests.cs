// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Search;
using NuGet.Services.Test.Common;
using NuGet.Services.V3Repository;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest
{
    public class DatabaseSearchResultsFormatterUnitTests
    {
        private const string Older = "Older";

        private readonly DatabaseSearchResultsFormatter _formatter;
        private readonly V3PathGenerator _pathGenerator;

        public DatabaseSearchResultsFormatterUnitTests()
        {
            _pathGenerator = new V3PathGenerator(new Uri("http://api.nuget.org/stage/123/"));
            _formatter = new DatabaseSearchResultsFormatter(_pathGenerator);
        }

        public static IEnumerable<object[]> _formatTestList => new[]
        {
            #region Single package
            new object[]
            {
                new List<PackageMetadata>
                {
                    DataMockHelper.CreateDefaultPackageMetadata()
                },
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    {
                        "data", new JArray
                        {
                            new JObject
                            {
                                {"@id", "http://api.nuget.org/stage/123/registration/json/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray {"nuget", "nuget2"}},
                                {"description", TestPackage.DefaultDescription},
                                {"iconUrl", TestPackage.DefaultIconUrl},
                                {"id", TestPackage.DefaultId},
                                {"licenseUrl", TestPackage.DefaultLicenseUrl},
                                {"projectUrl", TestPackage.DefaultProjectUrl},
                                {"registration", "http://api.nuget.org/stage/123/registration/json/index.json"},
                                {"tags", new JArray {"nuget", "test"}},
                                {"title", TestPackage.DefaultTitle},
                                {"totalDownloads", 0},
                                {"version", TestPackage.DefaultVersion},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id", "http://api.nuget.org/stage/123/registration/json/1.0.0.json"},
                                            {"downloads", 0},
                                            {"version", TestPackage.DefaultVersion}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },

            #endregion
                    
            #region Empty results
            new object[]
            {
                new List<PackageMetadata>(),
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    {"data", new JArray()}
                }
            },
            #endregion

            #region Two packages with same id
            new object[]
            {
                new List<PackageMetadata>
                {
                    new PackageMetadata
                    {
                        Authors = TestPackage.DefaultAuthors + Older,
                        Description = TestPackage.DefaultDescription + Older,
                        IconUrl = TestPackage.DefaultIconUrl + Older,
                        Id = TestPackage.DefaultId,
                        LicenseUrl = TestPackage.DefaultLicenseUrl + Older,
                        Version = TestPackage.DefaultVersion,
                        Owners = TestPackage.DefaultOwners + Older,
                        ProjectUrl = TestPackage.DefaultProjectUrl + Older,
                        Tags = TestPackage.DefaultTags + Older,
                        Summary = TestPackage.DefaultSummary + Older,
                        Title = TestPackage.DefaultTitle + Older,
                        StageKey = 1,
                        IsPrerelease = true
                    },
                    new PackageMetadata
                    {
                        Authors = TestPackage.DefaultAuthors,
                        Description = TestPackage.DefaultDescription,
                        IconUrl = TestPackage.DefaultIconUrl,
                        Id = TestPackage.DefaultId,
                        LicenseUrl = TestPackage.DefaultLicenseUrl,
                        Version = "1.0.0.1-beta",
                        Owners = TestPackage.DefaultOwners,
                        ProjectUrl = TestPackage.DefaultProjectUrl,
                        Tags = TestPackage.DefaultTags,
                        Summary = TestPackage.DefaultSummary,
                        Title = TestPackage.DefaultTitle,
                        StageKey = 1,
                        IsPrerelease = true
                    }

                },
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    {
                        "data", new JArray
                        {
                            new JObject
                            {
                                {"@id", "http://api.nuget.org/stage/123/registration/json/index.json"},
                                {"@type", "Package"},
                                {"authors", new JArray { "nuget", "nuget2" }},
                                {"description", TestPackage.DefaultDescription},
                                {"iconUrl", TestPackage.DefaultIconUrl},
                                {"id", TestPackage.DefaultId},
                                {"licenseUrl", TestPackage.DefaultLicenseUrl},
                                {"projectUrl", TestPackage.DefaultProjectUrl},
                                {"registration", "http://api.nuget.org/stage/123/registration/json/index.json"},
                                {"tags", new JArray {"nuget", "test"}},
                                {"title", TestPackage.DefaultTitle},
                                {"totalDownloads", 0},
                                {"version", "1.0.0.1-beta"},
                                {
                                    "versions", new JArray
                                    {
                                        new JObject
                                        {
                                            {"@id", "http://api.nuget.org/stage/123/registration/json/1.0.0.json"},
                                            {"downloads", 0},
                                            {"version", TestPackage.DefaultVersion}
                                        },
                                        new JObject
                                        {
                                            {"@id", "http://api.nuget.org/stage/123/registration/json/1.0.0.1-beta.json"},
                                            {"downloads", 0},
                                            {"version", "1.0.0.1-beta"}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            #endregion
        };

        [Theory]
        [MemberData("_formatTestList")]
        public void VerifyFormat(List<PackageMetadata> input, JObject expectedOutput)
        {
            // Act 
            var result = _formatter.FormatResults(input);

            // Assert
            var comparer = JObject.EqualityComparer;
            comparer.Equals(result, expectedOutput).Should().BeTrue();
        }
    }
}
