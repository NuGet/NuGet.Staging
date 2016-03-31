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
using Stage.Authentication;
using Stage.Database.Models;
using Stage.Manager.Authentication;
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
        private readonly IV3ServiceFactory _v3ServiceFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthenticationCredentialsExtractor _authenticationCredentialsExtractor;

        public PackageController(ILogger<PackageController> logger, StageContext context, IPackageService packageService, IStageService stageService,
                                IV3ServiceFactory v3ServiceFactory, IAuthenticationService authenticationService, IAuthenticationCredentialsExtractor authenticationCredentialsExtractor)
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

            if (v3ServiceFactory == null)
            {
                throw new ArgumentNullException(nameof(v3ServiceFactory));
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
            _packageService = packageService;
            _stageService = stageService;
            _v3ServiceFactory = v3ServiceFactory;
            _authenticationService = authenticationService;
            _authenticationCredentialsExtractor = authenticationCredentialsExtractor;
        }

        [HttpPut("{id:guid}")]
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> PushPackageToStage(string id)
        {
            var userInformation = await GetUserInformation();
            if (userInformation == null)
            {
                return new HttpUnauthorizedResult();
            }

            var stage = _stageService.GetStage(id);
            if (stage == null)
            {
                return new HttpNotFoundResult();
            }

            if (!_stageService.IsUserMemberOfStage(stage, userInformation.UserKey))
            {
                return new HttpUnauthorizedResult();
            }

            using (var packageStream = this.Request.Form.Files[0].OpenReadStream())
            {
                var v3Service = _v3ServiceFactory.Create(stage.Id);
                NuGet.V3Repository.IPackageMetadata packageMetadata = null;

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
                if (_stageService.DoesPackageExistsOnStage(stage, registrationId, normalizedVersion))
                {
                    return new ObjectResult(string.Format(PackageExistsOnStageMessage, registrationId, normalizedVersion, stage.DisplayName))
                    {
                        StatusCode = (int)HttpStatusCode.Conflict
                    };
                }

                // Check if user can write to this registration id
                if (!await _packageService.IsUserOwnerOfPackageAsync(userInformation.UserKey, registrationId))
                {
                    return new ObjectResult(ApiKeyUnauthorizedMessage) { StatusCode = (int)HttpStatusCode.Forbidden };
                }
               
                Uri nupkgUri = await v3Service.AddPackage(packageStream, packageMetadata);

                stage.Packages.Add(new StagedPackage()
                {
                    Id = registrationId,
                    NormalizedVersion = normalizedVersion,
                    Version = version.ToString(),
                    UserKey = userInformation.UserKey,
                    Published = DateTime.UtcNow,
                    NupkgUrl = nupkgUri.ToString()
                });

                await _context.SaveChangesAsync();

                // Check if package exists in the Gallery (warning message if so)
                bool packageAlreadyExists =
                    await _packageService.DoesPackageExistsAsync(registrationId, normalizedVersion);

                return packageAlreadyExists ?
                    new ObjectResult(string.Format(PackageAlreadyExists, registrationId, normalizedVersion))
                    {
                                StatusCode = (int)HttpStatusCode.Created
                    }
                    : (IActionResult)new HttpStatusCodeResult((int)HttpStatusCode.Created);
            }
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
