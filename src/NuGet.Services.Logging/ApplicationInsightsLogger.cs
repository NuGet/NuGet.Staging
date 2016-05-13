// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace NuGet.Services.Logging
{
    public class ApplicationInsightsLogger : ILogger
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly string _name;

        public ApplicationInsightsLogger(TelemetryClient telemetryClient, string name)
        {
            _telemetryClient = telemetryClient;
            _name = name;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;

            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else
            {
                if (state != null)
                {
                    message += state;
                }
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            if (!string.IsNullOrEmpty(message))
            {
                _telemetryClient.TrackTrace(message, GetSeverityLevel(logLevel));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // TODO: add support for log levels if u really want to
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NoopDisposable();
        }

        private static SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return SeverityLevel.Verbose;
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
