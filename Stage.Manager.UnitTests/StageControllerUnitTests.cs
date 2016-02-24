// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Stage.Manager.Controllers;
using Xunit;

namespace Stage.Manager.UnitTests
{

    /// <summary>
    /// Helpful links: https://blogs.msdn.microsoft.com/webdev/2015/08/06/unit-testing-with-dnx-asp-net-5-projects/
    /// </summary>
    public class StageControllerUnitTests
    {
        private StageController _stageController;

        public StageControllerUnitTests()
        {
            _stageController = new StageController((ILogger<StageController>)new DebugLogger(nameof(StageControllerUnitTests)), new StageContextMock().Object);
        }

        [Fact]
        public void UniTest1()
        {
            Assert.True(true);
        }
    }

}
