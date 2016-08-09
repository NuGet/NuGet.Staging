// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;

namespace NuGet.CommandLine.StagingExtensions
{
    [Command(resourceType: typeof(StagingResources), commandName: "stage", descriptionResourceName: "StageCommandDescription",
       MinArgs = 1, MaxArgs = 2, UsageSummaryResourceName = "StageCommandUsageSummary", UsageExampleResourceName = "StageCommandUsageExample")]
    public class StageCommand : Command
    {
        [Option(typeof(StagingResources), "StageCommandCreateDescription")]
        public string Create { get; set; }

        [Option(typeof(StagingResources), "StageCommandDropDescription")]
        public string Drop { get; set; }

        [Option(typeof(StagingResources), "StageCommandListDescription")]
        public string List { get; set; }

        [Option(typeof(StagingResources), "StageCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(StagingResources), "StageCommandApiKeyDescription")]
        public string ApiKey { get; set; }

        private enum SubCommand
        {
            None,
            Create,
            Drop,
            List
        }

        public override async Task ExecuteCommandAsync()
        {
            // Verify input
            SubCommand subCommand;
            string parameter;

            if (!TryParseArguments(out subCommand, out parameter))
            {
                HelpCommand.ViewHelpForCommand(CommandAttribute.CommandName);
                return;
            }

            // Get source - from command line param or default from config file
            string source = ResolveSource();

            // Get api key - from command line param or from config file
            string apiKey = GetApiKey(source);

            var stageManagementResource = await GetStageManagementResource(source);

            if (subCommand == SubCommand.Create)
            {
                await CreateStage(stageManagementResource, apiKey, parameter);
            }
            else if (subCommand == SubCommand.Drop)
            {
                await DropStage(stageManagementResource, apiKey, parameter);
            }
            else if (subCommand == SubCommand.List)
            {
                await ListStages(stageManagementResource, apiKey);
            }
        }

        private bool TryParseArguments(out SubCommand subCommand, out string parameter)
        {
            parameter = string.Empty;
            string commandString = Arguments[0];

            if (!Enum.TryParse(commandString, ignoreCase: true, result: out subCommand))
            {
                subCommand = SubCommand.None;
            }

            if (subCommand == SubCommand.Create || subCommand == SubCommand.Drop)
            {
                if (Arguments.Count < 2)
                {
                    return false;
                }

                parameter = Arguments[1];
            }

            return subCommand != SubCommand.None;
        }

        private async Task CreateStage(StageManagementResource stageManagementResource, string apiKey, string stageDisplayName)
        {
            Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.CreatingStageMessage, stageDisplayName));

            var createResult = await stageManagementResource.Create(stageDisplayName, apiKey, Console);

            Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.CreatedStageMessage, createResult.Id, createResult.Feed));
        }

        private async Task DropStage(StageManagementResource stageManagementResource, string apiKey, string stageId)
        {
            if (!NonInteractive)
            {
                if (!Console.Confirm(string.Format(CultureInfo.CurrentCulture, StagingResources.ConfirmStageDrop, stageId)))
                {
                    Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.DropCommandWasCanceled));
                    return;
                }
            }

            Console.LogWarning(string.Format(CultureInfo.CurrentCulture, StagingResources.DropingStageMessage, stageId));

            var dropResults = await stageManagementResource.Drop(stageId, apiKey, Console);

            Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.DroppedStageMessage, dropResults.Id));
        }

        private async Task ListStages(StageManagementResource stageManagementResource, string apiKey)
        {
            var listResult = await stageManagementResource.List(apiKey, Console);

            if (!listResult.Any())
            {
                Console.LogInformation(StagingResources.StageListNoStagesFound);
            }
            else
            {
                foreach (var stageListView in listResult)
                {
                    Console.WriteLine();
                    Console.PrintJustified(0, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageName, stageListView.DisplayName));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageStatus, stageListView.Status));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageFeed, stageListView.Feed));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageCreationDate, stageListView.CreationDate));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageExpirationDate, stageListView.ExpirationDate));
                }
            }
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
