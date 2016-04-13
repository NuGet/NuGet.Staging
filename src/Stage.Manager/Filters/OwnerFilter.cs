// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace Stage.Manager.Filters
{
    public class OwnerFilter : ActionFilterAttribute
    {
        private readonly IStageService _stageService;

        public OwnerFilter(IStageService stageService)
        {
            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            _stageService = stageService;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            // We assume this filter is called after StageIdFilter, and "stage" parameter exists and set
            var stage = (Database.Models.Stage)actionContext.ActionArguments["stage"];

            int userKey = GetUserKey(actionContext.HttpContext);

            if (!_stageService.IsUserMemberOfStage(stage, userKey))
            {
                actionContext.Result =  new HttpUnauthorizedResult();
            }
        }

        private int GetUserKey(HttpContext context)
        {
            return int.Parse(context.User.Identity.Name);
        }
    }
}