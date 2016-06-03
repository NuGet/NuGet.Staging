// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NuGet.Services.Staging.Search;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest.SearchUnitTests
{
    public class DatabaseAutocompleteResultsFormatterUnitTests
    {
        private DatabaseAutocompleteResultsFormatter _formatter;

        public DatabaseAutocompleteResultsFormatterUnitTests()
        {
            _formatter = new DatabaseAutocompleteResultsFormatter();
        }

        public static IEnumerable<object[]> _formatTestList => new []
        {
            new object[]
            {
                new List<string> { "abc", "cbd" },
                10,
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    { "totalHits", 10 },
                    { "index", "DB" },
                    { "data", new JArray("abc","cbd") }
                }
            },
            new object[]
            {
                new List<string>(),
                0,
                new JObject
                {
                    {"@context", new JObject {{"@vocab", "http://schema.nuget.org/schema#"}}},
                    { "totalHits", 0 },
                    { "index", "DB" },
                    { "data", new JArray() }
                }
            }
        };

        [Theory]
        [MemberData("_formatTestList")]
        public void VerifyFormat(List<string> input, int totalHits, JObject expectedOutput)
        {
            // Act 
            var result = _formatter.FormatResults(input, totalHits);

            // Assert
            // Verify lastReopen exists, and remove it because we can't mock DateTime.UtcNow
            result["lastReopen"].Should().NotBeNull();
            result.Remove("lastReopen");
            var comparer = JObject.EqualityComparer;
            comparer.Equals(result, expectedOutput).Should().BeTrue();
        }
    }
}
