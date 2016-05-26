// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Services.Staging.Authentication;
using NuGet.Versioning;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;
using static NuGet.Services.Staging.Manager.Controllers.Messages;

namespace NuGet.Services.Staging.Manager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PackageController : Controller
    {
        internal static readonly NuGetVersion MaxSupportedMinClientVersion = new NuGetVersion("3.5.0.0");

        private readonly ILogger<PackageController> _logger;
        private readonly IPackageService _packageService;
        private readonly IStageService _stageService;
        private readonly IV3ServiceFactory _v3ServiceFactory;

        public PackageController(ILogger<PackageController> logger, IPackageService packageService, IStageService stageService,
                                IV3ServiceFactory v3ServiceFactory)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (packageService == null)
            {
                throw new ArgumentNullException(nameof(packageService));
            }

            if (stageService == null)
            {
                throw new ArgumentNullException(nameof(stageService));
            }

            if (v3ServiceFactory == null)
            {
                throw new ArgumentNullException(nameof(v3ServiceFactory));
            }

            _logger = logger;
            _packageService = packageService;
            _stageService = stageService;
            _v3ServiceFactory = v3ServiceFactory;
        }

        [HttpPut("{id:guid}")]
        [HttpPost("{id:guid}")]
        [EnsureStageExists]
        [EnsureUserIsOwnerOfStage]
        public async Task<IActionResult> PushPackageToStage(Stage stage)
        {
            var userKey = GetUserInformation().UserKey;

            if (!_stageService.IsStageEditAllowed(stage))
            {
                return new BadRequestObjectResult(string.Format(StageEditNotAllowed, stage.DisplayName));
            }

            using (var packageStream = this.Request.Form.Files[0].OpenReadStream())
            {
                var v3Service = _v3ServiceFactory.Create(stage.Id);
                V3Repository.IPackageMetadata packageMetadata = null;

                try
                {
                    packageMetadata = v3Service.ParsePackageStream(packageStream);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    return new BadRequestObjectResult(string.Format(PackageErrorMessage, ex.Message));
                }

                NuspecReader nuspec;

                try
                {
                    nuspec = new NuspecReader(packageMetadata.Nuspec);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    return new BadRequestObjectResult(string.Format(NuspecErrorMessage, ex.Message));
                }

                // Check client version
                if (nuspec.GetMinClientVersion() > MaxSupportedMinClientVersion)
                {
                    return new BadRequestObjectResult(string.Format(MinClientVersionOutOfRangeMessage, nuspec.GetMinClientVersion()));
                }

                string registrationId = nuspec.GetId();
                var version = nuspec.GetVersion();
                string normalizedVersion = version.ToNormalizedString();

                // Check if package exists in the stage
                if (_stageService.DoesPackageExistOnStage(stage, registrationId, normalizedVersion))
                {
                    return new ObjectResult(string.Format(PackageExistsOnStageMessage, registrationId, normalizedVersion, stage.DisplayName))
                    {
                        StatusCode = (int)HttpStatusCode.Conflict
                    };
                }

                // Check if user can write to this registration id
                if (!await _packageService.IsUserOwnerOfPackageAsync(userKey, registrationId))
                {
                    return new ObjectResult(ApiKeyUnauthorizedMessage) { StatusCode = (int)HttpStatusCode.Conflict };
                }
               
                var packageLocations = await v3Service.AddPackage(packageStream, packageMetadata);

                await _stageService.AddPackageToStage(
                    stage,
                    new StagedPackage
                    {
                        Id = registrationId,
                        NormalizedVersion = normalizedVersion,
                        Version = version.ToString(),
                        UserKey = userKey,
                        Published = DateTime.UtcNow,
                        NupkgUrl = packageLocations.Nupkg.ToString(),
                        NuspecUrl = packageLocations.Nuspec.ToString(),
                        PackageMetadata = new PackageMetadata().LoadFromNuspec(nuspec, GetUserInformation())
                    }
                  );

                // Check if package exists in the Gallery (warning message if so)
                bool packageAlreadyExists =
                    await _packageService.DoesPackageExistsAsync(registrationId, normalizedVersion);

                return packageAlreadyExists ?
                    new ObjectResult(string.Format(PackageAlreadyExists, registrationId, normalizedVersion))
                    {
                                StatusCode = (int)HttpStatusCode.Created
                    }
                    : (IActionResult)new StatusCodeResult((int)HttpStatusCode.Created);
            }
        }

        private UserInformation GetUserInformation()
        {
            return (UserInformation)HttpContext.Items[Constants.UserInformationKey];
        }
    }
}
