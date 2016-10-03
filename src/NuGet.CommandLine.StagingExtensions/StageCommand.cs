// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Client.Staging;
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
        /// <summary>
        /// How often do we refresh commit progress in the begining
        /// </summary>
        private const int CommitProgressRefreshIntervalInitialSeconds = 10;

        /// <summary>
        /// Upper bound for commit progress refresh wait
        /// </summary>
        private const int CommitProgressRefreshIntervalUpperBoundSeconds = 60;

        /// <summary>
        /// Default commit progress monitoring timeout.
        /// </summary>
        private const int DefaultCommitProgressTimeoutSeconds = 300;

        internal enum CommitStatus
        {
            Pending,
            InProgress,
            Completed,
            Failed
        }

        [Option(typeof(StagingResources), "StageCommandCreateDescription")]
        public string Create { get; set; }

        [Option(typeof(StagingResources), "StageCommandDropDescription")]
        public string Drop { get; set; }

        [Option(typeof(StagingResources), "StageCommandListDescription")]
        public string List { get; set; }

        [Option(typeof(StagingResources), "StageCommandGetDescription")]
        public string Get { get; set; }

        [Option(typeof(StagingResources), "StageCommandCommitDescription")]
        public string Commit { get; set; }

        [Option(typeof(StagingResources), "StageCommandProgressDescription")]
        public string Progress { get; set; }

        [Option(typeof(StagingResources), "StageCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(StagingResources), "StageCommandApiKeyDescription")]
        public string ApiKey { get; set; }

        [Option(typeof(StagingResources), "StageCommandProgressTimeoutDescription")]
        public int Timeout { get; set; }

        private enum SubCommand
        {
            None,
            Create,
            Drop,
            List,
            Get,
            Commit,
            Progress
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

            var stageManagementResource = await GetStageManagementResource(source);

            if (subCommand == SubCommand.Get)
            {
                await GetStageDetails(stageManagementResource, parameter);
            }
            else if (subCommand == SubCommand.Progress)
            {
                await GetCommitProgress(stageManagementResource, parameter);
            }
            else
            {
                // Get api key - from command line param or from config file
                string apiKey = GetApiKey(source);

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
                else if (subCommand == SubCommand.Commit)
                {
                    await CommitStage(stageManagementResource, apiKey, parameter);
                }
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

            if (subCommand != SubCommand.None && subCommand != SubCommand.List)
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

            Console.LogWarning(string.Format(CultureInfo.CurrentCulture, StagingResources.DroppingStageMessage, stageId));

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
                    Console.PrintJustified(0, string.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", StagingResources.HeaderStageName, stageListView.DisplayName, stageListView.Id));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageStatus, stageListView.Status));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageFeed, stageListView.Feed));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageCreationDate, stageListView.CreationDate));
                    Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageExpirationDate, stageListView.ExpirationDate));
                }
            }
        }

        private async Task GetStageDetails(StageManagementResource stageManagementResource, string stageId)
        {
            var stageDetails = await stageManagementResource.GetStageDetails(stageId, Console);

            Console.WriteLine();
            Console.PrintJustified(0, string.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", StagingResources.HeaderStageName, stageDetails.DisplayName, stageDetails.Id));
            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageStatus, stageDetails.Status));
            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageFeed, stageDetails.Feed));
            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageCreationDate, stageDetails.CreationDate));
            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageExpirationDate, stageDetails.ExpirationDate));
            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}:", StagingResources.HeaderStageMemberships));

            foreach (var membership in stageDetails.Memberships)
            {
                Console.PrintJustified(2, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageMemberUserName, membership.Name));
                Console.PrintJustified(2, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", StagingResources.HeaderStageMemberType, membership.MembershipType));
            }

            Console.PrintJustified(1, string.Format(CultureInfo.CurrentCulture, "{0}:", StagingResources.HeaderStagePackages));

            if (stageDetails.PackagesCount > 0)
            {
                foreach (var package in stageDetails.Packages)
                {
                    Console.PrintJustified(2, string.Format(CultureInfo.CurrentCulture, "{0}, {1}", package.Id, package.Version));
                }
            }
            else
            {
                Console.PrintJustified(2, string.Format(CultureInfo.CurrentCulture, "{0}", StagingResources.None));
            }
        }

        private async Task GetCommitProgress(StageManagementResource stageManagementResource, string stageId)
        {
            int refreshSeconds = CommitProgressRefreshIntervalInitialSeconds;
            int monitoringTimeoutSeconds = Timeout <= 0 ? DefaultCommitProgressTimeoutSeconds : Timeout; 

            bool done = false;
            var startTime = DateTime.UtcNow;

            while (!done)
            {
                var progress = await stageManagementResource.GetCommitProgress(stageId, Console);
                CommitStatus commitStatus;

                if (!Enum.TryParse(progress.CommitStatus, ignoreCase: true, result: out commitStatus))
                {
                    Console.WriteWarning(string.Format(CultureInfo.CurrentCulture, "{0}",
                        StagingResources.CanNotParseProgress));
                    done = true;
                }
                else
                {
                    switch (commitStatus)
                    {
                        case CommitStatus.Pending:
                        {
                            Console.PrintJustified(0,
                                string.Format(CultureInfo.CurrentCulture, "{0}.", StagingResources.StageCommitPending));

                            break;
                        }
                        case CommitStatus.InProgress:
                        {
                            Console.PrintJustified(0,
                                string.Format(CultureInfo.CurrentCulture, "{0}:", StagingResources.StageCommitInProgress));

                            PrintPackagesProgress(progress);
                            Console.WriteLine();

                            break;
                        }
                        case CommitStatus.Completed:
                        {
                            Console.PrintJustified(0,
                                string.Format(CultureInfo.CurrentCulture, "{0}.", StagingResources.StageCommitCompleted));
                            done = true;

                            break;
                        }
                        case CommitStatus.Failed:
                        {
                            Console.WriteError(string.Format(CultureInfo.CurrentCulture, "{0}. Error: {1}",
                                StagingResources.StageCommitFailed, progress.ErrorMessage));
                            PrintPackagesProgress(progress);
                            Console.WriteWarning(string.Format(CultureInfo.CurrentCulture, "{0}.", StagingResources.RetryCommitMessage));
                            done = true;

                            break;
                        }
                    }

                    // Check for timeout
                    if ((DateTime.UtcNow - startTime).TotalSeconds > monitoringTimeoutSeconds)
                    {
                        done = true;
                    }

                    if (!done)
                    {
                        // Print ... until next retry.
                        for (int i = 0; i < refreshSeconds; i++)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            Console.Write(".");
                        }

                        refreshSeconds = CalculateNextCommitProgressRefreshInterval(refreshSeconds);

                        Console.WriteLine();
                    }
                }
            }
        }

        private int CalculateNextCommitProgressRefreshInterval(int commitProgressRefreshSeconds)
        {
            return Math.Min(CommitProgressRefreshIntervalUpperBoundSeconds,
                            CommitProgressRefreshIntervalInitialSeconds*(commitProgressRefreshSeconds/CommitProgressRefreshIntervalInitialSeconds+1));
        }

        private async Task CommitStage(StageManagementResource stageManagementResource, string apiKey, string stageId)
        {
            if (!NonInteractive)
            {
                if (!Console.Confirm(string.Format(CultureInfo.CurrentCulture, StagingResources.ConfirmStageCommit, stageId)))
                {
                    Console.LogInformation(string.Format(CultureInfo.CurrentCulture, StagingResources.CommitCommandWasCanceled));
                    return;
                }
            }

            Console.LogWarning(string.Format(CultureInfo.CurrentCulture, StagingResources.CommittingStageMessage, stageId));

            await stageManagementResource.Commit(stageId, apiKey, Console);

            await GetCommitProgress(stageManagementResource, stageId);
        }

        private void PrintPackagesProgress(StageCommitProgressView progress)
        {
            foreach (var packageProgress in progress.PackageProgressList)
            {
                Console.PrintJustified(1, string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}, {1}: {2}",
                    packageProgress.Id,
                    packageProgress.Version,
                    GetCommitStatusString(packageProgress.Progress)));
            }
        }

        internal static string GetCommitStatusString(string commitStatusString)
        {
            CommitStatus commitStatus;

            // If the server was updated, lets do our best to output the correct result for the new commit status:
            // Fallback to the received value, so that server changes won't break the client 
            if (!Enum.TryParse(commitStatusString, ignoreCase: true, result: out commitStatus))
            {
                return commitStatusString;
            }
            else if (commitStatus == CommitStatus.Pending)
            {
                return StagingResources.Pending;
            }
            else if (commitStatus == CommitStatus.InProgress)
            {
                return StagingResources.InProgress;
            }
            else if (commitStatus == CommitStatus.Completed)
            {
                return StagingResources.Completed;
            }
            else if (commitStatus == CommitStatus.Failed)
            {
                return StagingResources.Failed;
            }
            
            // This should never happen
            throw new ArgumentException($"Commit status value is not supported: {commitStatusString}");
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
