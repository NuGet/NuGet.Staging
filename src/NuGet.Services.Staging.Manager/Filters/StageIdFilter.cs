// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NuGet.Services.Staging.Manager.Filters
{
    public class StageIdFilterServiceAttribute : ServiceFilterAttribute
    {
        public StageIdFilterServiceAttribute() : base(typeof(StageIdFilter))
        {
        }
    }

    public class StageIdFilter : ActionFilterAttribute
    {
        public const string StageKeyName = "stage";

        public StageIdFilter()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var stageService = (IStageService)actionContext.HttpContext.RequestServices.GetService(typeof (IStageService));
            var inputStage = (Database.Models.Stage)actionContext.ActionArguments[StageKeyName];

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
