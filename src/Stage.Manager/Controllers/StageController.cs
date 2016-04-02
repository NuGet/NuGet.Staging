// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.Database.Models;
using Stage.Manager.Search;
using Stage.Manager.V3;
using static Stage.Manager.Controllers.Messages;

namespace Stage.Manager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class StageController : Controller
    {
        private readonly ILogger<StageController> _logger;
        private readonly StageContext _context;
        private readonly IStageService _stageService;
        private readonly StageIndexBuilder _stageIndexBuilder = new StageIndexBuilder();
        private readonly ISearchService _searchService;
        private readonly StorageFactory _storageFactory;

        private const string MessageFormat = "User: {UserKey}, Stage: {StageId}, {Message}";

        public StageController(ILogger<StageController> logger, StageContext context, IStageService stageService,
                               StorageFactory storageFactory, ISearchService searchService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
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

            _logger = logger;
            _context = context;
            _stageService = stageService;
            _searchService = searchService;
            _storageFactory = storageFactory;
        }

        // GET: api/stage
        [HttpGet]
        public async Task<IActionResult> ListUserStages()
        {
            var userKey = GetUserKey();
            var userMemberships = _context.StageMembers.Where(sm => sm.UserKey == userKey);

            return
                new HttpOkObjectResult(userMemberships.Select(sm => new ListViewStage(sm.Stage, sm, GetBaseAddress())).ToList());
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

            await _stageService.DropStage(stage);
            stage.Status = StageStatus.Deleted;

            _logger.LogInformation(MessageFormat, userKey, id, "Drop was successful");
            return new HttpOkObjectResult(new ViewStage(stage, GetBaseAddress()));
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Commit(string id)
        {
            // Not implemented
            return new BadRequestResult();
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

        public class DetailedViewStage : ViewStage
        {
            public DetailedViewStage(Database.Models.Stage stage, string baseAddress) : base(stage, baseAddress)
            {
                Packages = new List<ViewPackage>(stage.Packages.Select(package => new ViewPackage(package)));
                PackagesCount = Packages.Count;
                Members = new List<ViewMember>(stage.Members.Select(member => new ViewMember(member)));
            }

            public int PackagesCount { get; set; }
            public List<ViewPackage> Packages { get; set; }
            public List<ViewMember> Members { get; set; } 
        }

        public class ListViewStage : ViewStage
        {
            public ListViewStage(Database.Models.Stage stage, StageMember member, string baseAddress) : base(stage, baseAddress)
            {
                MemberType = member.MemberType.ToString();
            }

            public string MemberType { get; set; }
        }
        
        public class ViewStage
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Status { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime ExpirationDate { get; set; }
            public string Feed { get; set; }

            public ViewStage(Database.Models.Stage stage, string baseAddress)
            {
                Id = stage.Id;
                DisplayName = stage.DisplayName;
                CreationDate = stage.CreationDate;
                ExpirationDate = stage.ExpirationDate;
                Status = stage.Status.ToString();
                Feed = $"{baseAddress}/api/stage/{stage.Id}/index.json";
            }
        }

        public class ViewPackage
        {
            public ViewPackage(StagedPackage package)
            {
                Id = package.Id;
                Version = package.Version;
            }

            public string Id { get; set; }
            public string Version { get; set; }
        }

        public class ViewMember
        {
            public ViewMember(StageMember member)
            {
                Name = member.UserKey.ToString();
                MemberType = member.MemberType.ToString();
            }

            // TODO: now this is user key, but change to actual user name
            public string Name { get; set; }
            public string MemberType { get; set; }
        }
    }
}
