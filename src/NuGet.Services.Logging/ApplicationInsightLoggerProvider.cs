﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace NuGet.Services.Logging
{
    /// <summary>
    /// Based on this code: https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.EventLog/EventLogLoggerProvider.cs
    /// </summary>
    public class ApplicationInsightLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightLoggerProvider(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(_telemetryClient, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
