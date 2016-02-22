// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Stage.Database.Models;

namespace Stage.Manager.Controllers
{
    [Route("api/[controller]")]
    public class StageController : Controller
    {
        // After this period the stage will expire and be deleted
        public const int DefaultExpirationPeriodDays = 30;

        private readonly ILogger<StageController> _logger;
        private readonly StageContext _context;

        // Temp user key until we add authentication + autherization
        private const int _userKey = 101;

        public StageController(ILogger<StageController> logger, StageContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: api/stage
        [HttpGet]
        public IActionResult ListUserStages()
        {
            var userKey = GetUserKey();
            var userMemberships = _context.StageMembers.Where(sm => sm.UserKey == userKey);

            _logger.LogVerbose("User: {0}, List stages was requested. Found {1} stages.", userKey, userMemberships.Count());

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
                            }));
        }

        // GET api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpGet("{id}")]
        public IActionResult GetDetails(string id)
        {
            var userKey = GetUserKey();

            var stage = _context.Stages.FirstOrDefault(s => s.Id == id);

            if (stage == null)
            {
                _logger.LogVerbose(FormatLog(userKey, id, "Can't retrieve stage details. Stage not found."));
                return new HttpNotFoundResult();
            }

            _logger.LogVerbose(FormatLog(userKey, id, "Stage details retrieved successfuly."));
            return new HttpOkObjectResult(new { stage.Id, stage.DisplayName, stage.CreationDate, stage.ExpirationDate, stage.Status });
        }

        // POST api/stage
        [HttpPost()]
        public async Task<IActionResult> Create([FromBody]string displayName)
        {
            var userKey = GetUserKey();

            _logger.LogVerbose(FormatLog(userKey, displayName, "Create stage was requested"));

            if (!CheckStageDisplayNameValidity(displayName))
            {
                _logger.LogInformation(FormatLog(userKey, displayName, "Create stage failed due to invalid display name"));
                return new BadRequestObjectResult("Provide a non-empty display name");
            }

            var stage = new Stage.Database.Models.Stage
            {
                StageMemebers = new List<StageMemeber>(new[]
                {
                    new StageMemeber()
                    {
                        MemberType = MemberType.Owner,
                        UserKey = userKey
                    }
                }),
                Id = Guid.NewGuid().ToString("N"),
                DisplayName = displayName,
                CreationDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow + TimeSpan.FromDays(DefaultExpirationPeriodDays),
                Status = StageStatus.Active,
            };

            _context.Stages.Add(stage);
            await _context.SaveChangesAsync();

            _logger.LogInformation(FormatLog(userKey, displayName, "Create stage succeeded. Id: " + stage.Id));

            // TODO: add feed uri to returned data
            return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id, stage.ExpirationDate, stage.Status });
        }

        // DELETE api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpDelete("{id}")]
        public async Task<IActionResult> Drop(string id)
        {
            // TODO: in the future, just mark the stage as deleted and have a background job perform the actual delete
            var userKey = GetUserKey();

            _logger.LogVerbose(FormatLog(userKey, id, "Drop was requested"));

            var stage = _context.Stages.FirstOrDefault(s => s.Id == id);

            if (stage != null)
            {
                _context.Stages.Remove(stage);
                await _context.SaveChangesAsync();

                _logger.LogInformation(FormatLog(userKey, id, "Drop was successful"));
                return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id });
            }

            _logger.LogInformation(FormatLog(userKey, id, "Drop failed, stage not found"));
            return new HttpNotFoundResult();
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id}")]
        public async Task<IActionResult> Commit(string id)
        {
            // Not implemented
            return new BadRequestResult();
        }

        #region Private Methods

        private string FormatLog(int userKey, string stage, string message)
        {
            return string.Format("User {0}, Stage: {1}, {2}", userKey, stage, message);
        }

        private bool CheckStageDisplayNameValidity(string displayName)
        {
            return !string.IsNullOrWhiteSpace(displayName);
        }

        private int GetUserKey()
        {
            return _userKey;
        } 

        #endregion
    }
}
