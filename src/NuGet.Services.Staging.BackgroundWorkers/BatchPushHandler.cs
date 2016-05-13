// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Newtonsoft.Json;
using NuGet.Resolver;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;
using NuGet.Versioning;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class BatchPushHandler : IMessageHandler<PackageBatchPushData> 
    {
        private const string LogDetails = "Stage id: {Stage} Package id: {Package} Version: {Version}";

        private readonly ILogger<StageCommitWorker> _logger;
        private ICommitStatusService _commitStatusService;
        private readonly IPackageMetadataService _packageMetadataService;
        private readonly IPackagePushService _packagePushService;

        public BatchPushHandler(
            ICommitStatusService commitStatusService, IPackageMetadataService packageMetadataService,
            IPackagePushService packagePushService, ILogger<StageCommitWorker> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (commitStatusService == null)
            {
                throw new ArgumentNullException(nameof(commitStatusService));
            }

            if (packageMetadataService == null)
            {
                throw new ArgumentNullException(nameof(packageMetadataService));
            }

            if (packagePushService == null)
            {
                throw new ArgumentNullException(nameof(packagePushService));
            }

            _logger = logger;
            _commitStatusService = commitStatusService;
            _packageMetadataService = packageMetadataService;
            _packagePushService = packagePushService;
        }

        public async Task HandleMessageAsync(PackageBatchPushData pushData, bool isLastDelivery)
        {
            StageCommit stageCommit = _commitStatusService.GetCommit(pushData.StageId);

            if (stageCommit == null)
            {
                _logger.LogWarning("Commit data for stage {StageId} not found.", pushData.StageId);
                return;
            }

            if (stageCommit.Status == CommitStatus.Completed || stageCommit.Status == CommitStatus.Failed)
            {
                _logger.LogWarning("Commit status for stage {StageId} doesn't require handling. Status: {Status}.",
                    pushData.StageId, stageCommit.Status);
                return;
            }

            List<PackagePushData> sortedPackages = (await SortPackagesByPushOrder(pushData.PackagePushDataList)).ToList();
            BatchPushProgressReport progressReport = GetCommitProgressReport(stageCommit, pushData.PackagePushDataList);
            Dictionary<string, PackagePushProgressReport> commitProgressDictionary =
                progressReport.PackagePushProgressReports.ToDictionary(x => GetPackageKey(x.Id, x.Version));

            for (int i = 0; i < sortedPackages.Count && progressReport.Status != PushProgressStatus.Failed; i++)
            {
                var package = sortedPackages[i];
                var packageProgressReport = commitProgressDictionary[GetPackageKey(package.Id, package.Version)];

                try
                {
                    if (packageProgressReport.Status == PushProgressStatus.Pending)
                    {
                        _logger.LogTrace("Pushing: " + LogDetails, pushData.StageId, package.Id, package.Version);

                        await UpdateProgress(stageCommit, progressReport, packageProgressReport, PushProgressStatus.InProgress);

                        await PushPackageAndUpdateProgress(pushData, package, stageCommit, progressReport, packageProgressReport, succeedOnExists: false);
                    }
                    else if (packageProgressReport.Status == PushProgressStatus.InProgress)
                    {
                        // We continue a commit, and discover that package is in progress.
                        // We don't know if it was already pushed to Gallery or not. 
                        // Try to push, if fails on conflict, ignore. If success, update commit

                        _logger.LogTrace("Retrying push: " + LogDetails, pushData.StageId, package.Id,
                            package.Version);

                        await PushPackageAndUpdateProgress(pushData, package, stageCommit, progressReport, packageProgressReport, succeedOnExists: true);
                    }
                    else if (packageProgressReport.Status == PushProgressStatus.Completed)
                    {
                        _logger.LogInformation("Skipping push. Already pushed." + LogDetails, pushData.StageId,
                            package.Id, package.Version);
                    }
                    else if (packageProgressReport.Status == PushProgressStatus.Failed)
                    {
                        _logger.LogError("Unexpected failure status for package." + LogDetails, pushData.StageId,
                            package.Id, package.Version);
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(
                        LogLevel.Error,
                        0,
                        new FormattedLogValues("Unexpected exception was caught while commiting." + LogDetails, pushData.StageId, package.Id, package.Version),
                        e,
                        (values, exception) =>  values.ToString());

                    try
                    {
                        // This is the last delivery, so try to update the DB with failure
                        var status = isLastDelivery ? PushProgressStatus.Failed : PushProgressStatus.InProgress;

                        await UpdateProgress(stageCommit, progressReport, packageProgressReport, status, "Error:" + e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to update status.", ex);
                    }

                    throw;
                }
            }
        }

        private async Task PushPackageAndUpdateProgress(PackageBatchPushData pushData, PackagePushData package,
                                                        StageCommit stageCommit, BatchPushProgressReport progressReport,
                                                        PackagePushProgressReport packageProgressReport, bool succeedOnExists)
        {
            PackagePushResult result = await _packagePushService.PushPackage(package);

            _logger.LogInformation("Push completed with {@Result}" + LogDetails, result, pushData.StageId, package.Id, package.Version);

            if (result.Status == PackagePushStatus.Success ||
                (succeedOnExists && result.Status == PackagePushStatus.AlreadyExists))
            {
                await UpdateProgress(stageCommit, progressReport, packageProgressReport, PushProgressStatus.Completed);
            }
            else
            {
                await UpdateProgress(stageCommit, progressReport, packageProgressReport, PushProgressStatus.Failed, result.ErrorMessage);
            }
        }

        private async Task UpdateProgress(StageCommit commit, BatchPushProgressReport batchPushProgressReport,
                                          PackagePushProgressReport packagePushProgressReport, PushProgressStatus newStatus,
                                          string errorMessage = null)
        {
            packagePushProgressReport.Status = newStatus;

            if (newStatus == PushProgressStatus.Failed)
            {
                batchPushProgressReport.Status = PushProgressStatus.Failed;
                batchPushProgressReport.FailureDetails = errorMessage;
            }
            else if (newStatus == PushProgressStatus.Completed &&
                     batchPushProgressReport.PackagePushProgressReports.All(x => x.Status == PushProgressStatus.Completed))
            {
                batchPushProgressReport.Status = PushProgressStatus.Completed;
            }

            await _commitStatusService.UpdateProgress(commit, batchPushProgressReport);
        }

        private BatchPushProgressReport GetCommitProgressReport(StageCommit stageCommit, List<PackagePushData> pushedPackages)
        {
            BatchPushProgressReport progressReport;

            if (!string.IsNullOrEmpty(stageCommit.Progress))
            {
                progressReport = JsonConvert.DeserializeObject<BatchPushProgressReport>(stageCommit.Progress);

                _logger.LogInformation("Found commit progress report: {@progressReport}", progressReport);
            }
            else
            {
                progressReport = new BatchPushProgressReport
                {
                    Status = PushProgressStatus.InProgress,
                    PackagePushProgressReports = pushedPackages.Select(x => new PackagePushProgressReport
                    {
                        Id = x.Id,
                        Version = x.Version,
                        Status = PushProgressStatus.Pending
                    }).ToList()
                };
            }

            return progressReport;
        }

        private async Task<IReadOnlyList<PackagePushData>> SortPackagesByPushOrder(List<PackagePushData> packages)
        {
            var packagesDictionary = packages.ToDictionary(p => GetPackageKey(p.Id, p.Version));
            var packageIds = new HashSet<string>(packages.Select(p => p.Id));
            var resolverPackages = new List<ResolverPackage>();

            _logger.LogTrace($"Sorting {packages.Count} packages: {string.Join(", ", packagesDictionary.Keys)}");

            foreach (var package in packages)
            {
                var dependencies = await _packageMetadataService.GetPackageDependencies(package);

                // Filter the dependencies to contain only other packages in this Stage. We don't care about 
                // external packages in the push order
                var filteredDependencies = dependencies.Where(d => packageIds.Contains(d.Id));
                var resolverPackage = new ResolverPackage(package.Id, new NuGetVersion(package.Version), filteredDependencies, true, false);
                resolverPackages.Add(resolverPackage);
            }

            var sortedPackages = ResolverUtility.TopologicalSort(resolverPackages).ToList();

            _logger.LogTrace($"Sorted order: {string.Join(", ", sortedPackages.Select(x => GetPackageKey(x.Id, x.Version.ToString())))}");

            return sortedPackages.Select(p => packagesDictionary[GetPackageKey(p.Id, p.Version.ToString())]).ToImmutableList();
        }

        private string GetPackageKey(string id, string version)
        {
            return $"{id}-{version}";
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                (_commitStatusService as IDisposable)?.Dispose();
                _commitStatusService = null;
            }
        }
    }
}