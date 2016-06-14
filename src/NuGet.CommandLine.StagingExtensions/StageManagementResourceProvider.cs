// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;

namespace NuGet.CommandLine.StagingExtensions
{
    public class StageManagementResourceProvider : ResourceProvider
    {
        public static readonly string Version350 = "/3.5.0";
        public static readonly string StagingBaseAddress = "StagingService" + Version350;

        public StageManagementResourceProvider()
            : base(typeof(StageManagementResource),
                  nameof(StageManagementResourceProvider),
                  NuGetResourceProviderPositions.Last)
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
        {
            StageManagementResource stageManagementResource = null;

            var serviceIndex = await source.GetResourceAsync<ServiceIndexResourceV3>(token);

            if (serviceIndex != null)
            {
                var baseUrl = serviceIndex[StagingBaseAddress].FirstOrDefault();

                HttpSource httpSource = null;
                var sourceUri = baseUrl?.AbsoluteUri;

                if (!string.IsNullOrEmpty(sourceUri))
                {
                    var httpSourceResource = await source.GetResourceAsync<HttpSourceResource>(token);
                    httpSource = httpSourceResource.HttpSource;
                    stageManagementResource = new StageManagementResource(sourceUri, httpSource);
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture,
                        StagingResources.StagingNotSupported,
                        source));
                }
            }

            var result = new Tuple<bool, INuGetResource>(stageManagementResource != null, stageManagementResource);
            return result;
        }
    }
}
