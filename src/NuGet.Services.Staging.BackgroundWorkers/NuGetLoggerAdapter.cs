// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class NuGetLoggerAdapter : NuGet.Common.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _internalLogger;

        public NuGetLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _internalLogger = logger;
        }

        public void LogDebug(string data)
        {
            _internalLogger.LogDebug(data);
        }

        public void LogVerbose(string data)
        {
            _internalLogger.LogTrace(data);
        }

        public void LogInformation(string data)
        {
            _internalLogger.LogInformation(data);
        }

        public void LogMinimal(string data)
        {
            _internalLogger.LogInformation(data);
        }

        public void LogWarning(string data)
        {
            _internalLogger.LogWarning(data);
        }

        public void LogError(string data)
        {
            _internalLogger.LogError(data);
        }

        public void LogInformationSummary(string data)
        {
            _internalLogger.LogInformation(data);
        }

        public void LogErrorSummary(string data)
        {
            _internalLogger.LogInformation(data);
        }

        public void LogSummary(string data)
        {
            _internalLogger.LogInformation(data);
        }
    }
}