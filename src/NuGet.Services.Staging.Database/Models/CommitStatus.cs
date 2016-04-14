// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Staging.Database.Models
{
    public enum CommitStatus
    {
        Pending,    // Request was sent, but commit hasn't started yet
        InProgress, // Request is being processed
        Completed,  // Commit completed successfully
        Failed,     // Error occured
        TimedOut    // Commit progress wasn't updated for longer then allowed period
    }
}