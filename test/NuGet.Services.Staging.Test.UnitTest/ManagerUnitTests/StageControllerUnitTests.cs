// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.v3;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager;
using NuGet.Services.Staging.Manager.Controllers;
using NuGet.Services.Staging.PackageService;
using NuGet.Services.V3Repository;
using Xunit;

namespace NuGet.Services.Staging.Test.UnitTest
{
    /// <summary>
    /// Helpful links:
    /// https://blogs.msdn.microsoft.com/webdev/2015/08/06/unit-testing-with-dnx-asp-net-5-projects/
    /// http://www.jerriepelser.com/blog/unit-testing-controllers-aspnet5
    /// </summary>
    public class StageControllerUnitTests
    {
        protected const string DisplayName = "display name";
        protected const string TrackId = "trackId";
        protected UserInformation DefaultUser = new UserInformation { UserKey = 3, UserName = "testUser" };

        protected StageController _stageController;
        protected StageContextMock _stageContextMock;
        protected Mock<HttpContext> _httpContextMock;
        protected Mock<StageService> _stageServiceMock;
        protected Mock<IPackageService> _packageServiceMock;

        protected List<PackageBatchPushData> _pushedBatches = new List<PackageBatchPushData>(); 

        public StageControllerUnitTests()
        {
            // Arrange
            _stageContextMock = new StageContextMock();

            _stageServiceMock = new Mock<StageService>(_stageContextMock.Object)
            {
                CallBase = true
            };

            _stageServiceMock.Setup(x => x.GetStage(It.IsAny<string>()))
                .Returns((string id) => _stageContextMock.Object.Stages.FirstOrDefault(x => x.Id == id));

            _stageServiceMock.Setup(x => x.GetUserMemberships(It.IsAny<int>()))
                .Returns((int key) => _stageContextMock.Object.StageMemberships.Where(x => x.UserKey == key));

            _packageServiceMock = new Mock<IPackageService>();
            _packageServiceMock.Setup(x => x.PushBatchAsync(It.IsAny<PackageBatchPushData>())).Returns(Task.FromResult(TrackId))
                               .Callback<PackageBatchPushData>(x => _pushedBatches.Add(x));

            var v3ServiceFactory = new Mock<IV3ServiceFactory>();
            v3ServiceFactory
                .Setup(x => x.CreatePathCalculator(It.IsAny<string>()))
                .Returns<string>(stageId => new V3PathCalculator(new Uri($"https://api.nuget.org/{stageId}/")));

            _stageController = new StageController(
                new Mock<ILogger<StageController>>().Object,
                _stageServiceMock.Object,
                new Mock<ISearchServiceFactory>().Object,
                _packageServiceMock.Object,
                v3ServiceFactory.Object);

            _httpContextMock = _stageController.WithMockHttpContext().WithUser(DefaultUser).WithBaseAddress();
        }

        [Fact]
        public async Task WhenListUserStagesCalledStagesAreReturned()
        {
            // Arrange
            var stage1 = await AddMockStage("first");
            var stage2 = await AddMockStage("second");

            // Act
            IActionResult actionResult = _stageController.ListUserStages();

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();

            object result = (actionResult as OkObjectResult).Value;
            result.Should().BeOfType<List<ListViewStage>>();
            var stages = result as List<ListViewStage>;
            stages.Count.Should().Be(2);
            VerifyListViewStage(stages[0], stage1);
            VerifyListViewStage(stages[1], stage2);
        }

        [Fact]
        public void VerifyListRequiresAuthentication()
        {
            AuthorizationTest.IsAuthorized(_stageController, "ListUserStages", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public async Task WhenGetDetailsIsCalledDetailsAreReturned()
        {
            // Arrange
            var stage = await AddMockStage("first");
            var secondStage = await AddMockStage(DisplayName);
            _stageContextMock.AddMockPackage(secondStage, "package");
            
            // Act
            IActionResult actionResult = _stageController.GetDetails(secondStage);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();

            var result = (actionResult as OkObjectResult).Value;
            result.Should().BeOfType<DetailedViewStage>();

            var stageDetails = result as DetailedViewStage;

            VerifyDetailedViewStage(stageDetails, secondStage);
        }

        [Fact]
        public void VerifyGetDetailsIsAnonymous()
        {
            AuthorizationTest.IsAnonymous(_stageController, "GetDetails", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void WhenGetDetailsIsCalledWithNonExistingStageId404IsReturned()
        {
            AttributeHelper.HasServiceFilterAttribute<EnsureStageExistsFilter>(_stageController, "GetDetails", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void WhenIndexIsCalledJsonIsReturned()
        {
            // Act
            IActionResult actionResult = _stageController.Index(new Stage { Id = "330f56e4-7ca7-46b9-b53d-13c89ac15ba3" });

            // Assert
            actionResult.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult) actionResult;
            ((JObject) jsonResult.Value).ToString().Should().NotBeEmpty();
            var jsonObj = (JObject) jsonResult.Value;
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[0]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchQueryService[1]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.SearchAutocompleteService).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[0]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[1]).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.PackageBaseAddress).Should().NotBeEmpty();
            jsonObj["resources"].Where(x => x["@type"].ToString() == ServiceTypes.PackagePublish).Should().NotBeEmpty();

            var a = jsonObj["resources"].First(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[0]);
            jsonObj["resources"].First(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[0])["@id"].ToString().Should().Be("https://api.nuget.org/330f56e4-7ca7-46b9-b53d-13c89ac15ba3/registration/");
            jsonObj["resources"].First(x => x["@type"].ToString() == ServiceTypes.RegistrationsBaseUrl[1])["@id"].ToString().Should().Be("https://api.nuget.org/330f56e4-7ca7-46b9-b53d-13c89ac15ba3/registration/");
            jsonObj["resources"].First(x => x["@type"].ToString() == ServiceTypes.PackageBaseAddress)["@id"].ToString().Should().Be("https://api.nuget.org/330f56e4-7ca7-46b9-b53d-13c89ac15ba3/flatcontainer/");
        }

        [Fact]
        public void VerifyIndexIsAnonymous()
        {
            AuthorizationTest.IsAnonymous(_stageController, "Index", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void VerifyQueryIsAnonymous()
        {
            AuthorizationTest.IsAnonymous(_stageController, "Query", methodTypes: null).Should().BeTrue();
        }

        protected async Task<Stage> AddMockStage(string displayName)
        {
            IActionResult actionResult = await _stageController.Create(displayName);

            var stage = _stageContextMock.Object.Stages.Last();
            _stageContextMock.Object.StageMemberships.AddRange(stage.Memberships);

            foreach (var memberships in stage.Memberships)
            {
                memberships.Stage = stage;
            }
            stage.Packages = new List<StagedPackage>();
            stage.Commits = new List<StageCommit>();
            object result = (actionResult as OkObjectResult).Value;
            string id = (string)result.GetType().GetProperty("Id").GetValue(result);
            return _stageContextMock.Object.Stages.First(x => x.Id == id);
        }

        protected void VerifyPackagePush(PackagePushData actual, StagedPackage expected)
        {
            actual.Id.Should().Be(expected.Id, "Ids should match");
            actual.NupkgPath.Should().Be(expected.NupkgUrl, "nupkg url should match");
            actual.UserKey.Should().Be(expected.UserKey.ToString(), "user key should match");
            actual.Version.Should().Be(expected.NormalizedVersion, "versions should match");
        }

        protected void VerifyViewStage(ViewStage actual, Stage expected)
        {
            actual.Id.Should().Be(expected.Id);
            actual.CreationDate.Should().Be(expected.CreationDate);
            actual.DisplayName.Should().Be(expected.DisplayName);
            actual.ExpirationDate.Should().Be(expected.ExpirationDate);
            actual.Status.Should().Be(expected.Status.ToString());
        }

        protected void VerifyListViewStage(ListViewStage actual, Stage expected)
        {
            VerifyViewStage(actual, expected);
            actual.MembershipType.Should().Be(expected.Memberships.First().MembershipType.ToString());
        }

        protected void VerifyDetailedViewStage(DetailedViewStage actual, Stage expected)
        {
            VerifyViewStage(actual, expected);
            actual.PackagesCount.Should().Be(expected.Packages.Count);

            foreach (var package in expected.Packages)
            {
                var packageView = actual.Packages.FirstOrDefault(x => x.Id == package.Id && x.Version == package.NormalizedVersion);
                Assert.NotNull(packageView);
            }

            foreach (var membership in expected.Memberships)
            {
                var memberView = actual.Memberships.FirstOrDefault(x => x.Name == membership.UserKey.ToString());
                Assert.NotNull(memberView);
                memberView.MembershipType.Should().Be(membership.MembershipType.ToString());
            }
        }
    }
}
