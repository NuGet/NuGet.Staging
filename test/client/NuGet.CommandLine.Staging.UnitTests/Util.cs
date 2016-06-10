// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Configuration;
using Newtonsoft.Json.Linq;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.CommandLine.Staging.UnitTests
{
    public static class Util
    {
        public static string GetNuGetExePath()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Directory.GetCurrentDirectory();
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            return nugetexe;
        }

        public static JObject CreateIndexJson()
        {
            return JObject.Parse(@"{
                  ""version"": ""3.2.0"",
                  ""resources"": [],
                ""@context"": {
                ""@vocab"": ""http://schema.nuget.org/services#"",
                ""comment"": ""http://www.w3.org/2000/01/rdf-schema#comment""
                    }}");
        }

        public static void AddStagingResource(JObject index, MockServer server)
        {
            var resource = new JObject();
            resource.Add("@id", string.Format("{0}stage", server.Uri));
            resource.Add("@type", "StagingService/3.5.0");

            var array = index["resources"] as JArray;
            array.Add(resource);
        }

        public static void ClearWebCache()
        {
            var nugetexe = Util.GetNuGetExePath();

            var r = CommandRunner.Run(
            nugetexe,
            ".",
            "locals http-cache -Clear",
            waitForExit: true);

            Assert.Equal(0, r.Item1);
        }
    }
}
