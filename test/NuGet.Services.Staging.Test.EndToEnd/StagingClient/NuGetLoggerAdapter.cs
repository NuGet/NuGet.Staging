// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit.Abstractions;

namespace NuGet.Services.Staging.Test.EndToEnd
{
    public class NuGetLoggerAdapter : NuGet.Common.ILogger
    {
        private readonly ITestOutputHelper _outputHelper;

        public NuGetLoggerAdapter(ITestOutputHelper outputHelper)
        {
            if (outputHelper == null)
            {
                throw new ArgumentNullException(nameof(outputHelper));
            }

            _outputHelper = outputHelper;
        }

        public void LogDebug(string data)
        {
            _outputHelper.WriteLine("DEBUG: " + data);
        }

        public void LogVerbose(string data)
        {
            _outputHelper.WriteLine("VERBOSE: " + data);
        }

        public void LogInformation(string data)
        {
            _outputHelper.WriteLine("INFO: " + data);
        }

        public void LogMinimal(string data)
        {
            LogInformation(data);
        }

        public void LogWarning(string data)
        {
            _outputHelper.WriteLine("WARNING: " + data);
        }

        public void LogError(string data)
        {
            _outputHelper.WriteLine("ERROR: " + data);
        }

        public void LogInformationSummary(string data)
        {
            LogSummary(data);
        }

        public void LogErrorSummary(string data)
        {
            LogError(data);
        }

        public void LogSummary(string data)
        {
            LogInformation(data);
        }
    }
}