// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace Stage.Manager.Logging
{
    /// <summary>
    /// Based on this code: https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.EventLog/EventLogLoggerProvider.cs
    /// </summary>
    public class ApplicationInsightsLogger : ILogger
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly string _name;

        public ApplicationInsightsLogger(TelemetryClient telemetryClient, string name)
        {
            _telemetryClient = telemetryClient;
            _name = name;
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = _name + ": " + message;

            _telemetryClient.TrackTrace(message, GetSeverityLevel(logLevel));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // TODO: add support for log levels if u really want to
            return true;
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
        }

        private static SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
            case LogLevel.Verbose: return SeverityLevel.Verbose;
            case LogLevel.Debug: return SeverityLevel.Verbose;
            case LogLevel.Information: return SeverityLevel.Information;
            case LogLevel.None: return SeverityLevel.Information;
            case LogLevel.Warning: return SeverityLevel.Warning;
            case LogLevel.Error: return SeverityLevel.Error;
            case LogLevel.Critical: return SeverityLevel.Critical;
            default:
                throw new ArgumentException("Illegal LogLevel", nameof(logLevel));
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
