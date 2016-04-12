// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.Database.Models;
using Stage.Manager.Search;
using Stage.Manager.V3;
using Stage.Packages;
using static Stage.Manager.Controllers.Messages;

namespace Stage.Manager.Controllers
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

            return new HttpOkObjectResult(stageViews);
        }

        // GET api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public IActionResult GetDetails(string id)
        {
            var stage = _stageService.GetStage(id);
            if (stage == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(new DetailedViewStage(stage, GetBaseAddress()));
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

            return new HttpOkObjectResult(new ListViewStage(stage, stage.Members.First(), GetBaseAddress()));
        }

        // DELETE api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Drop(string id)
        {
            var userKey = GetUserKey();
            var stage = _stageService.GetStage(id);

            if (stage == null)
            {
                _logger.LogInformation(MessageFormat, userKey, id, "Drop failed, stage not found");
                return new HttpNotFoundResult();
            }

            if (!_stageService.IsUserMemberOfStage(stage, userKey))
            {
                return new HttpUnauthorizedResult();
            }

            if (stage.Status == StageStatus.Committing)
            {
                return new BadRequestObjectResult(string.Format(CommitInProgressMessage, stage.DisplayName));
            }

            await _stageService.DropStage(stage);
            stage.Status = StageStatus.Deleted;

            _logger.LogInformation(MessageFormat, userKey, id, "Drop was successful");
            return new HttpOkObjectResult(new ViewStage(stage, GetBaseAddress()));
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Commit(string id)
        {
            // TODO: this method must not be executed concurrently for the same stage, this will cause commit to
            // be triggered twice, and unexpected behavior. Will need to add a stage level lock to protect this.
             
            var userKey = GetUserKey();
            var stage = _stageService.GetStage(id);

            if (stage == null)
            {
                _logger.LogInformation(MessageFormat, userKey, id, "Drop failed, stage not found");
                return new HttpNotFoundResult();
            }

            if (!_stageService.IsUserMemberOfStage(stage, userKey))
            {
                return new HttpUnauthorizedResult();
            }

            if (stage.Packages.Count == 0)
            {
                return new BadRequestObjectResult(string.Format(EmptyStageCommitMessage, stage.DisplayName));
            }

            // 1. Check stage status - if already commiting, return error 
            if (stage.Status == StageStatus.Committing)
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

            _logger.LogInformation(MessageFormat, userKey, id, "Commit initiated successfully");

            return new HttpStatusCodeResult((int) HttpStatusCode.Created);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/commit")]
        public IActionResult GetCommitProgress(string id)
        {
            var stage = _stageService.GetStage(id);
            if (stage == null)
            {
                return new HttpNotFoundResult();
            }

            var commit = _stageService.GetCommit(stage);

            if (commit == null)
            {
                return new BadRequestResult();
            }

            var commitProgressView = CreateViewStageCommitProgress(stage, commit);

            return new HttpOkObjectResult(commitProgressView);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/index.json")]
        public IActionResult Index(string id)
        {
            var index = _stageIndexBuilder.CreateIndex(GetBaseAddress(), id, _storageFactory.BaseAddress);
            return Json(index);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/query")]
        public async Task<IActionResult> Query(string id)
        {
            var stage = _stageService.GetStage(id);
            if (stage == null)
            {
                return new HttpNotFoundResult();
            }

            if (_searchService is DummySearchService)
            {
                ((DummySearchService) _searchService).BaseAddress = new Uri(_storageFactory.BaseAddress, $"{id}/");
            }

            var searchResult = await _searchService.Search(id, Request.QueryString.Value);
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

        private PackageBatchPushData CreatePackageBatchPushData(Database.Models.Stage stage)
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
                            UserKey = p.UserKey.ToString()
                        }).ToList(),
                    StageId = stage.Id
                };
        }

        private ViewStageCommitProgress CreateViewStageCommitProgress(Database.Models.Stage stage, StageCommit commit)
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
