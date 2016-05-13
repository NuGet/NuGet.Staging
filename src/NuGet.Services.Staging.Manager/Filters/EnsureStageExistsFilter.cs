// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager
{
    public class EnsureStageExistsFilter : ActionFilterAttribute
    {
        public const string StageKeyName = "stage";

        public EnsureStageExistsFilter()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var stageService = (IStageService)actionContext.HttpContext.RequestServices.GetService(typeof (IStageService));
            var inputStage = (Stage)actionContext.ActionArguments[StageKeyName];

            var stage = stageService.GetStage(inputStage.Id);

            if (stage == null)
            {
                actionContext.Result = new NotFoundResult();
            }
            else
            {
                actionContext.ActionArguments[StageKeyName] = stage;
            }
        }
    }
}
