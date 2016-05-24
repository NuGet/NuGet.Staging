// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager.Search;
using NuGet.Services.Staging.Manager.V3;
using NuGet.Services.Staging.PackageService;
using static NuGet.Services.Staging.Manager.Controllers.Messages;

namespace NuGet.Services.Staging.Manager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class StageController : Controller
    {
        private readonly ILogger<StageController> _logger;
        private readonly IStageService _stageService;
        private readonly StageIndexBuilder _stageIndexBuilder = new StageIndexBuilder();
        private readonly ISearchService _searchService;
        private readonly StorageFactory _storageFactory;
        private readonly IPackageService _packageService;

        private const string MessageFormat = "User: {UserKey}, Stage: {StageId}, {Message}";

        public StageController(ILogger<StageController> logger, IStageService stageService,
                               StorageFactory storageFactory, ISearchService searchService, IPackageService packageService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            if (searchService == null)
            {
                throw new ArgumentNullException(nameof(searchService));
            }

            if (storageFactory == null)
            {
                throw new ArgumentNullException(nameof(storageFactory));
            }

            if (packageService == null)
            {
                throw new ArgumentNullException(nameof(packageService));
            }

            _logger = logger;
            _stageService = stageService;
            _searchService = searchService;
            _storageFactory = storageFactory;
            _packageService = packageService;
        }

        // GET: api/stage
        [HttpGet]
        public IActionResult ListUserStages()
        {
            var userKey = GetUserKey();
            var userMemberships =_stageService.GetUserMemberships(userKey).ToList();
            var stageViews = userMemberships.Select(sm => new ListViewStage(sm.Stage, sm, GetBaseAddress())).ToList();

            return new OkObjectResult(stageViews);
        }

        // GET api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [EnsureStageExists]
        public IActionResult GetDetails(Stage stage)
        {
            return new OkObjectResult(new DetailedViewStage(stage, GetBaseAddress()));
        }

        // POST api/stage
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]string displayName)
        {
            if (!_stageService.CheckStageDisplayNameValidity(displayName))
            {
                return new BadRequestObjectResult(string.Format(InvalidStageDisplayName, StageService.MaxDisplayNameLength));
            }

            var userKey = GetUserKey();
            var stage = await _stageService.CreateStage(displayName, userKey);

            _logger.LogInformation(MessageFormat, userKey, stage.Id, "Create stage succeeded. Display name: " + stage.DisplayName);

            return new OkObjectResult(new ListViewStage(stage, stage.Memberships.First(), GetBaseAddress()));
        }

        // DELETE api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpDelete("{id:guid}")]
        [EnsureStageExists]
        [EnsureUserIsOwnerOfStage]
        public async Task<IActionResult> Drop(Stage stage)
        {
            var userKey = GetUserKey();

            if (stage.Status == StageStatus.Committing)
            {
                return new BadRequestObjectResult(string.Format(CommitInProgressMessage, stage.DisplayName));
            }

            await _stageService.DropStage(stage);

            _logger.LogInformation(MessageFormat, userKey, stage.Id, "Drop was successful");
            return new OkObjectResult(new ViewStage(stage, GetBaseAddress()));
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id:guid}")]
        [EnsureStageExists]
        [EnsureUserIsOwnerOfStage]
        public async Task<IActionResult> Commit(Stage stage)
        {
            // TODO: https://github.com/NuGet/NuGet.Staging/issues/32

            if (stage.Packages.Count == 0)
            {
                return new BadRequestObjectResult(string.Format(EmptyStageCommitMessage, stage.DisplayName));
            }

            // 1. Check stage status - if already commiting, return error 
            if (stage.Status == StageStatus.Committing || stage.Status == StageStatus.Committed)
            {
                return new ObjectResult(string.Format(CommitInProgressMessage, stage.DisplayName))
                {
                    StatusCode = (int) HttpStatusCode.Conflict
                };
            }

            // 2. Prepare push metadata - all data needed for push without access to stage DB
            var pushData = CreatePackageBatchPushData(stage);

            // 3. Give to a PackageManager component
            string trackingId = await _packageService.PushBatchAsync(pushData);

            // 4. Save tracking id in the DB
            await _stageService.CommitStage(stage, trackingId);

            _logger.LogInformation(MessageFormat, GetUserKey(), stage.Id, "Commit initiated successfully");

            return new StatusCodeResult((int) HttpStatusCode.Created);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/commit")]
        [EnsureStageExists]
        public IActionResult GetCommitProgress(Stage stage)
        {
            var commit = _stageService.GetCommit(stage);

            if (commit == null)
            {
                return new BadRequestObjectResult(string.Format(Messages.CommitNotFound, stage.DisplayName));
            }

            var commitProgressView = CreateViewStageCommitProgress(stage, commit);

            return new OkObjectResult(commitProgressView);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/index.json")]
        [EnsureStageExists]
        public IActionResult Index(Stage stage)
        {
            var index = _stageIndexBuilder.CreateIndex(GetBaseAddress(), stage.Id, _storageFactory.BaseAddress);
            return Json(index);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/query")]
        [EnsureStageExists]
        public async Task<IActionResult> Query(Stage stage)
        {
            if (_searchService is DummySearchService)
            {
                ((DummySearchService) _searchService).BaseAddress = new Uri(_storageFactory.BaseAddress, $"{stage.Id}/");
            }

            var searchResult = await _searchService.Search(stage.Id, Request.QueryString.Value);
            return new JsonResult(searchResult);
        }

        private int GetUserKey()
        {
            return int.Parse(HttpContext.User.Identity.Name);
        }

        private string GetBaseAddress()
        {
            return $"{Request.Scheme}://{Request.Host.Value}";
        }

        private PackageBatchPushData CreatePackageBatchPushData(Stage stage)
        {
            return
                new PackageBatchPushData
                {
                    PackagePushDataList = stage.Packages.Select(
                        p => new PackagePushData
                        {
                            Id = p.Id,
                            Version = p.Version,
                            NupkgPath = p.NupkgUrl,
                            NuspecPath = p.NuspecUrl,
                            UserKey = p.UserKey.ToString()
                        }).ToList(),
                    StageId = stage.Id
                };
        }

        private ViewStageCommitProgress CreateViewStageCommitProgress(Stage stage, StageCommit commit)
        {
            BatchPushProgressReport progressReport = _stageService.GetCommitProgress(commit);
            var commitProgressView = new ViewStageCommitProgress(stage, GetBaseAddress());

            if (progressReport != null)
            {
                commitProgressView.CommitStatus = progressReport.Status.ToString();
                commitProgressView.ErrorMessage = progressReport.FailureDetails;
                commitProgressView.PackageProgressList =
                    progressReport.PackagePushProgressReports.Select(p => new ViewPackageCommitProgress
                    {
                        Id = p.Id,
                        Version = p.Version,
                        Progress = p.Status.ToString()
                    }).ToList();
            }
            else
            {
                commitProgressView.CommitStatus = commit.Status.ToString();
                commitProgressView.PackageProgressList = stage.Packages.Select(p => new ViewPackageCommitProgress
                {
                    Id = p.Id,
                    Version = p.Version,
                    Progress = PushProgressStatus.Pending.ToString()
                }).ToList();
            }

            return commitProgressView;
        }
    }
}
