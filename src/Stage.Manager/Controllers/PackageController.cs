// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Versioning;
using Stage.Database.Models;
using Stage.Packages;
using static Stage.Manager.Controllers.Messages;

namespace Stage.Manager.Controllers
{
    [Route("api/[controller]")]
    public class PackageController : Controller
    {
        internal static readonly NuGetVersion MaxSupportedMinClientVersion = new NuGetVersion("3.4.0.0");

        private readonly ILogger<PackageController> _logger;
        private readonly StageContext _context;
        private readonly IPackageService _packageService;
        private readonly IStageService _stageService;

        public PackageController(ILogger<PackageController> logger, StageContext context, IPackageService packageService, IStageService stageService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (packageService == null)
            {
                throw new ArgumentNullException(nameof(packageService));
            }

            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            _logger = logger;
            _context = context;
            _packageService = packageService;
            _stageService = stageService;
        }

        [HttpPut("{id:guid}")]
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> PushPackageToStage(string id)
        {
            var userKey = GetUserKey();

            var stage = _stageService.GetStage(id);
            if (stage == null || !_stageService.IsUserMemberOfStage(stage, userKey))
            {
                return new HttpNotFoundResult();
            }

            using (var packageStream = this.Request.Form.Files[0].OpenReadStream())
            {
                try
                {
            using (var packageToPush = new PackageArchiveReader(packageStream, leaveStreamOpen: false))
            {
                NuspecReader nuspec = null;
                try
                {
                    nuspec = new NuspecReader(packageToPush.GetNuspec());
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    return new BadRequestObjectResult(string.Format(NuspecErrorMessage, ex.Message));
                }

                // Check client version
                if (nuspec.GetMinClientVersion() > MaxSupportedMinClientVersion)
                {
                    return
                        new BadRequestObjectResult(string.Format(MinClientVersionOutOfRangeMessage,
                            nuspec.GetMinClientVersion()));
                }

                string registrationId = nuspec.GetId();
                var version = nuspec.GetVersion();
                string normalizedVersion = version.ToNormalizedString();

                // Check if package exists in the stage
                if (_stageService.DoesPackageExistsOnStage(stage, registrationId, normalizedVersion))
                {
                    return
                                new ObjectResult(string.Format(PackageExistsOnStageMessage, registrationId,
                                    normalizedVersion, stage.DisplayName)) { StatusCode = (int)HttpStatusCode.Conflict };
                }

                // Check if user can write to this registration id
                if (!await _packageService.IsUserOwnerOfPackageAsync(userKey, registrationId))
                {
                            return new ObjectResult(ApiKeyUnauthorizedMessage) { StatusCode = (int)HttpStatusCode.Forbidden };
                }

                stage.Packages.Add(new StagedPackage()
                {
                    Id = registrationId,
                    NormalizedVersion = normalizedVersion,
                    Version = version.ToString(),
                    UserKey = userKey,
                    Published = DateTime.UtcNow,
                            NupkgUrl = "TBD"
                });

                await _context.SaveChangesAsync();

                // Check if package exists in the Gallery (warning message if so)
                bool packageAlreadyExists =
                    await _packageService.DoesPackageExistsAsync(registrationId, normalizedVersion);

                return packageAlreadyExists
                    ? new ObjectResult(string.Format(PackageAlreadyExists, registrationId, normalizedVersion))
                    {
                                StatusCode = (int)HttpStatusCode.Created
                            }
                            : (IActionResult)new HttpStatusCodeResult((int)HttpStatusCode.Created);
                    }
                }
                catch (InvalidDataException ex)
                {
                    return new BadRequestObjectResult(string.Format(PackageErrorMessage, ex.Message));
                    }
                
            }
        }

        private int GetUserKey() => 1;
    }
}
