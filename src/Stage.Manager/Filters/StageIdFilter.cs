// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace Stage.Manager.Filters
{
    public class StageIdFilter : ActionFilterAttribute
    {
        public const string StageKeyName = "stage";

        private readonly IStageService _stageService;

        public StageIdFilter(IStageService stageService)
        {
            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            _stageService = stageService;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var inputStage =  (Database.Models.Stage)actionContext.ActionArguments[StageKeyName];

            var stage = _stageService.GetStage(inputStage.Id);

            if (stage == null)
            {
                actionContext.Result = new HttpNotFoundResult();
            }
            else
            {
                actionContext.ActionArguments[StageKeyName] = stage;
            }
        }
    }
}
