// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.v3;
using NuGet.Services.Metadata.Catalog.Persistence;
using Stage.Database.Models;
using Stage.Manager.Controllers;
using Stage.Manager.Filters;
using Stage.Manager.Search;
using Stage.Packages;
using Xunit;

namespace Stage.Manager.UnitTests
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
        protected const int UserKey = 3;

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
                .Returns((int key) => _stageContextMock.Object.StageMembers.Where(x => x.UserKey == key));

            _packageServiceMock = new Mock<IPackageService>();
            _packageServiceMock.Setup(x => x.PushBatchAsync(It.IsAny<PackageBatchPushData>())).Returns(Task.FromResult(TrackId))
                               .Callback<PackageBatchPushData>(x => _pushedBatches.Add(x));

            _stageController = new StageController(
                new Mock<ILogger<StageController>>().Object,
                _stageServiceMock.Object,
                new Mock<StorageFactory>().Object,
                new Mock<ISearchService>().Object,
                _packageServiceMock.Object);

            _httpContextMock = _stageController.WithMockHttpContext().WithUser(UserKey).WithBaseAddress();
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
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            object result = (actionResult as HttpOkObjectResult).Value;
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
            AddMockPackage(secondStage, "package");
            
            // Act
            IActionResult actionResult = _stageController.GetDetails(secondStage);

            // Assert
            actionResult.Should().BeOfType<HttpOkObjectResult>();

            var result = (actionResult as HttpOkObjectResult).Value;
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
            AttributeHelper.HasServiceFilterAttribute<StageIdFilter>(_stageController, "GetDetails", methodTypes: null).Should().BeTrue();
        }

        [Fact]
        public void WhenIndexIsCalledJsonIsReturned()
        {
            // Act
            IActionResult actionResult = _stageController.Index(new Database.Models.Stage { Id = Guid.NewGuid().ToString() });

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

        protected async Task<Database.Models.Stage> AddMockStage(string displayName)
        {
            IActionResult actionResult = await _stageController.Create(displayName);

            var stage = _stageContextMock.Object.Stages.Last();
            _stageContextMock.Object.StageMembers.AddRange(stage.Members);

            foreach (var member in stage.Members)
            {
                member.Stage = stage;
            }
            stage.Packages = new List<StagedPackage>();
            stage.Commits = new List<StageCommit>();
            object result = (actionResult as HttpOkObjectResult).Value;
            string id = (string)result.GetType().GetProperty("Id").GetValue(result);
            return _stageContextMock.Object.Stages.First(x => x.Id == id);
        }

        protected StagedPackage AddMockPackage(Database.Models.Stage stage, string packageId)
        {
            const string version = "1.0.0";
            var package = new StagedPackage
            {
                Id = packageId,
                Version = version,
                NormalizedVersion = version,
                NupkgUrl = $"http://api.nuget.org/{stage.Id}/{packageId}/{version}/{packageId}.{version}.nupkg",
                UserKey = UserKey
            };

            stage.Packages.Add(package);
            return package;
        }

        protected void VerifyPackagePush(PackagePushData actual, StagedPackage expected)
        {
            actual.Id.Should().Be(expected.Id, "Ids should match");
            actual.NupkgPath.Should().Be(expected.NupkgUrl, "nupkg url should match");
            actual.UserKey.Should().Be(expected.UserKey.ToString(), "user key should match");
            actual.Version.Should().Be(expected.NormalizedVersion, "versions should match");
        }

        protected void VerifyViewStage(ViewStage actual, Database.Models.Stage expected)
        {
            actual.Id.Should().Be(expected.Id);
            actual.CreationDate.Should().Be(expected.CreationDate);
            actual.DisplayName.Should().Be(expected.DisplayName);
            actual.ExpirationDate.Should().Be(expected.ExpirationDate);
            actual.Status.Should().Be(expected.Status.ToString());
        }

        protected void VerifyListViewStage(ListViewStage actual, Database.Models.Stage expected)
        {
            VerifyViewStage(actual, expected);
            actual.MemberType.Should().Be(expected.Members.First().MemberType.ToString());
        }

        protected void VerifyDetailedViewStage(DetailedViewStage actual, Database.Models.Stage expected)
        {
            VerifyViewStage(actual, expected);
            actual.PackagesCount.Should().Be(expected.Packages.Count);

            foreach (var package in expected.Packages)
            {
                var packageView = actual.Packages.FirstOrDefault(x => x.Id == package.Id && x.Version == package.NormalizedVersion);
                Assert.NotNull(packageView);
            }

            foreach (var member in expected.Members)
            {
                var memberView = actual.Members.FirstOrDefault(x => x.Name == member.UserKey.ToString());
                Assert.NotNull(memberView);
                memberView.MemberType.Should().Be(member.MemberType.ToString());
            }
        }
    }
}
