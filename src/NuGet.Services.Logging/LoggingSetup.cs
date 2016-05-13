// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace NuGet.Services.Logging
{
    public static class LoggingSetup
    {
        public static ILoggerFactory CreateLoggerFactory()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new ApplicationInsightLoggerProvider(new TelemetryClient()));

            return loggerFactory;
        }
    }
}
