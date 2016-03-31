// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.Authentication;
using Stage.Database.Models;
using Stage.Manager.Authentication;
using Stage.Manager.Search;
using Stage.Manager.V3;
using static Stage.Manager.Controllers.Messages;

namespace Stage.Manager.Controllers
{

    [Route("api/[controller]")]
    public class StageController : Controller
    {
        private readonly ILogger<StageController> _logger;
        private readonly StageContext _context;
        private readonly IStageService _stageService;
        private readonly StageIndexBuilder _stageIndexBuilder = new StageIndexBuilder();
        private readonly ISearchService _searchService;
        private readonly StorageFactory _storageFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthenticationCredentialsExtractor _authenticationCredentialsExtractor;

        private const string MessageFormat = "User: {UserKey}, Stage: {StageId}, {Message}";

        public StageController(ILogger<StageController> logger, StageContext context, IStageService stageService,
                               StorageFactory storageFactory, ISearchService searchService, IAuthenticationService authenticationService,
                               IAuthenticationCredentialsExtractor authenticationCredentialsExtractor)
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

            if (authenticationService == null)
            {
                throw new ArgumentNullException(nameof(authenticationService));
            }

            if (authenticationCredentialsExtractor == null)
            {
                throw new ArgumentNullException(nameof(authenticationCredentialsExtractor));
            }

            _logger = logger;
            _context = context;
            _stageService = stageService;
            _searchService = searchService;
            _storageFactory = storageFactory;
            _authenticationService = authenticationService;
            _authenticationCredentialsExtractor = authenticationCredentialsExtractor;
        }

        // GET: api/stage
        [HttpGet]
        public async Task<IActionResult> ListUserStages()
        {
            var userInformation = await GetUserInformation();
            if (userInformation == null)
            {
                return new HttpUnauthorizedResult();
            }

            var userMemberships = _context.StageMembers.Where(sm => sm.UserKey == userInformation.UserKey);

            return
                new HttpOkObjectResult(
                    userMemberships.Select(
                        sm =>
                            new
                            {
                                sm.MemberType,
                                sm.Stage.Id,
                                sm.Stage.CreationDate,
                                sm.Stage.ExpirationDate,
                                sm.Stage.DisplayName,
                                sm.Stage.Status
                            }).ToList());
        }

        // GET api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpGet("{id:guid}")]
        public IActionResult GetDetails(string id)
        {
            var stage = _stageService.GetStage(id);
            if (stage == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(new { stage.Id, stage.DisplayName, stage.CreationDate, stage.ExpirationDate, stage.Status });
        }

        // POST api/stage
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]string displayName)
        {
            var userInformation = await GetUserInformation();
            if (userInformation == null)
            {
                return new HttpUnauthorizedResult();
            }

            if (!_stageService.CheckStageDisplayNameValidity(displayName))
            {
                return new BadRequestObjectResult(string.Format(InvalidStageDisplayName, StageService.MaxDisplayNameLength));
            }

            var stage = await _stageService.CreateStage(displayName, userInformation.UserKey);

            _logger.LogInformation(MessageFormat, userInformation.UserKey, stage.Id, "Create stage succeeded. Display name: " + stage.DisplayName);

            // TODO: add feed uri to returned data
            return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id, stage.ExpirationDate, stage.Status });
        }

        // DELETE api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Drop(string id)
        {
            var userInformation = await GetUserInformation();
            if (userInformation == null)
            {
                return new HttpUnauthorizedResult();
            }

            var stage = _stageService.GetStage(id);

            if (stage == null)
            {
                _logger.LogInformation(MessageFormat, userInformation.UserKey, id, "Drop failed, stage not found");
                return new HttpNotFoundResult();
            }

            if (!_stageService.IsUserMemberOfStage(stage, userInformation.UserKey))
            {
                return new HttpUnauthorizedResult();
            }

            await _stageService.DropStage(stage);

            _logger.LogInformation(MessageFormat, userInformation.UserKey, id, "Drop was successful");
            return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id });
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Commit(string id)
        {
            // Not implemented
            return new BadRequestResult();
        }

        [HttpGet("{id:guid}/index.json")]
        public IActionResult Index(string id)
        {
            var index = _stageIndexBuilder.CreateIndex(Request.Scheme, Request.Host.Value, id, _storageFactory.BaseAddress);
            return Json(index);
        }

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

        private async Task<IUserInformation> GetUserInformation()
        {
            var credentials = _authenticationCredentialsExtractor.GetCredentials(Request);

            if (credentials != null)
            {
                return await _authenticationService.Authenticate(credentials);
            }

            return null;
        }
    }
}
