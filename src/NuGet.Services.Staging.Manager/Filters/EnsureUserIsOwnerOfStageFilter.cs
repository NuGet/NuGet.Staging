// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager
{
    public class EnsureUserIsOwnerOfStageFilter : ActionFilterAttribute
    {
        public EnsureUserIsOwnerOfStageFilter()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var stageService = (IStageService)actionContext.HttpContext.RequestServices.GetService(typeof(IStageService));

            // We assume this filter is called after EnsureStageExistsFilter, and "stage" parameter exists and set
            var stage = (Stage)actionContext.ActionArguments["stage"];

            int userKey = GetUserKey(actionContext.HttpContext);

            if (!stageService.IsStageMember(stage, userKey))
            {
                actionContext.Result =  new UnauthorizedResult();
            }
        }

        private int GetUserKey(HttpContext context)
        {
            return int.Parse(context.User.Identity.Name);
        }
    }
}