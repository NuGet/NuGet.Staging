// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Logging;
using Stage.Database.Models;
using static Stage.Manager.Controllers.Messages;
using static Stage.Manager.StageHelper;

namespace Stage.Manager.Controllers
{

    [Route("api/[controller]")]
    public class StageController : Controller
    {
        // After this period the stage will expire and be deleted
        public const int DefaultExpirationPeriodDays = 30;
        public const int MaxDisplayNameLength = 32;

        private readonly ILogger<StageController> _logger;
        private readonly StageContext _context;

        private const string MessageFormat = "User: {UserKey}, Stage: {StageId}, {Message}";

        public StageController(ILogger<StageController> logger, StageContext context)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger = logger;
            _context = context;
        }

        // GET: api/stage
        [HttpGet]
        public IActionResult ListUserStages()
        {
            var userKey = GetUserKey();
            var userMemberships = _context.StageMembers.Where(sm => sm.UserKey == userKey);

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
        [HttpGet("{id}")]
        public IActionResult GetDetails(string id)
        {
            var userKey = GetUserKey();

            if (!VerifyStageId(id))
            {
                return new BadRequestObjectResult(InvalidStageIdMessage);
            }

            var stage = GetStage(id);
            if (stage == null || !stage.IsUserMemberOfStage(userKey))
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(new { stage.Id, stage.DisplayName, stage.CreationDate, stage.ExpirationDate, stage.Status });
        }

        // POST api/stage
        [HttpPost()]
        public async Task<IActionResult> Create([FromBody]string displayName)
        {
            var userKey = GetUserKey();

            if (!CheckStageDisplayNameValidity(displayName))
            {
                return new BadRequestObjectResult(string.Format(InvalidStageDisplayName, MaxDisplayNameLength));
            }

            var utcNow = DateTime.UtcNow;

            var stage = new Database.Models.Stage
            {
                Members = new List<StageMember>(new[]
                {
                    new StageMember()
                    {
                        MemberType = MemberType.Owner,
                        UserKey = userKey
                    }
                }),
                Id = GuidToStageId(Guid.NewGuid()),
                DisplayName = displayName,
                CreationDate = utcNow,
                ExpirationDate = utcNow.AddDays(DefaultExpirationPeriodDays),
                Status = StageStatus.Active,
            };

            _context.Stages.Add(stage);
            await _context.SaveChangesAsync();

            _logger.LogInformation(MessageFormat, userKey, stage.Id, "Create stage succeeded. Display name: " + stage.DisplayName);

            // TODO: add feed uri to returned data
            return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id, stage.ExpirationDate, stage.Status });
        }

        // DELETE api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpDelete("{id}")]
        public async Task<IActionResult> Drop(string id)
        {
            var userKey = GetUserKey();

            if (!VerifyStageId(id))
            {
                return new BadRequestObjectResult(InvalidStageIdMessage);
            }
           
            var stage = GetStage(id);
            if (stage == null || !stage.IsUserMemberOfStage(userKey))
            {
                _logger.LogInformation(MessageFormat, userKey, id, "Drop failed, stage not found");
                return new HttpNotFoundResult();
            }

            // TODO: in the future, just mark the stage as deleted and have a background job perform the actual delete
            _context.Stages.Remove(stage);
            await _context.SaveChangesAsync();

            _logger.LogInformation(MessageFormat, userKey, id, "Drop was successful");
            return new HttpOkObjectResult(new { stage.DisplayName, stage.CreationDate, stage.Id });
        }

        // POST api/stage/e92156e2d6a74a19853a3294cf681dfc
        [HttpPost("{id}")]
        public async Task<IActionResult> Commit(string id)
        {
            // Not implemented
            return new BadRequestResult();
        }

        private bool CheckStageDisplayNameValidity(string displayName)
        {
            return !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= MaxDisplayNameLength;
        }

        private static string GuidToStageId(Guid guid) => guid.ToString("N");

        /// <summary>
        /// This method is virtual for test purposes. Include is an extension method, and hence, unmockable.
        /// </summary>
        public virtual Database.Models.Stage GetStage(string stageId) =>
            _context.Stages.Include(s => s.Members).Include(s => s.Packages).FirstOrDefault(s => s.Id == stageId);

        private int GetUserKey() => 1;
    }
}
