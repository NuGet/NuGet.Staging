// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace Stage.Manager.Logging
{
    public static class ApplicationInsightsLoggerExtensions
    {
        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory, TelemetryClient telemetryClient)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            factory.AddProvider(new ApplicationInsightLoggerProvider(telemetryClient));
            return factory;
        }
    }
}
