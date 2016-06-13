// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;

namespace NuGet.CommandLine.StagingExtensions
{
    [Command(resourceType: typeof(StagingResources), commandName: "stage", descriptionResourceName: "StageCommandDescription",
       MinArgs = 0, MaxArgs = 0, UsageSummaryResourceName = "StageCommandUsageSummary")]
    public class StageCommand : Command
    {
        [Option(typeof(StagingResources), "StageCommandCreateDescription")]
        public string Create { get; set; }

        [Option(typeof(StagingResources), "StageCommandDropDescription")]
        public string Drop { get; set; }

        [Option(typeof(StagingResources), "StageCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(StagingResources), "StageCommandApiKeyDescription")]
        public string ApiKey { get; set; }

        public override async Task ExecuteCommandAsync()
        {
            // Verify input
            if (string.IsNullOrEmpty(Create) == string.IsNullOrEmpty(Drop))
            {
                HelpCommand.ViewHelpForCommand(CommandAttribute.CommandName);
                return;
            }

            // Get source - from command line param or default from config file
            string source = ResolveSource();

            // Get api key - from command line param or from config file
            string apiKey = GetApiKey(source);

            var stageManagementResource = await GetStageManagementResource(source);

            if (!string.IsNullOrEmpty(Create))
            {
                await CreateStage(stageManagementResource, apiKey, Create);
            }
        }

        private async Task CreateStage(StageManagementResource stageManagementResource, string apiKey, string stageDisplayName)
        {
            Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.CreatingStageMessage, stageDisplayName));

            var createResult = await stageManagementResource.Create(stageDisplayName, apiKey, Console);

            Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.CreatedStageMessage, createResult.Feed));
        }

        private async Task<StageManagementResource> GetStageManagementResource(string source)
        {
            var sourceRepository = GetSourceRepository(new Configuration.PackageSource(source));
            var stageManagementResourceProvider = await sourceRepository.GetResourceAsync<StageManagementResource>();
            return stageManagementResourceProvider;
        }

        private SourceRepository GetSourceRepository(Configuration.PackageSource source)
        {
            var resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            resourceProviders.AddRange(FactoryExtensionsV2.GetCoreV3(Repository.Provider));
            resourceProviders.Add(new Lazy<INuGetResourceProvider>(() => new StageManagementResourceProvider()));

            return new SourceRepository(source, resourceProviders);
        }

        private string ResolveSource()
        {
            string source = Source;

            if (string.IsNullOrEmpty(source))
            {
                source = SourceProvider.DefaultPushSource;
            }

            if (!string.IsNullOrEmpty(source))
            {
                source = SourceProvider.ResolveAndValidateSource(source);
            }

            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException(NuGetResources.Error_MissingSourceParameter);
            }

            return source;
        }

        private string GetApiKey(string source)
        {
            string apiKey = ApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = SettingsUtility.GetDecryptedValue(Settings, ConfigurationConstants.ApiKeys, source);
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    StagingResources.NoApiKeyFound,
                    GetSourceDisplayName(source)));
            }
            return apiKey;
        }

        private static string GetSourceDisplayName(string source)
        {
            if (String.IsNullOrEmpty(source) || source.Equals(NuGetConstants.DefaultGalleryServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                return $"{StagingResources.LiveFeed} ({NuGetConstants.DefaultGalleryServerUrl})";
            }

            return $"'{source}'";
        }
    }
}
