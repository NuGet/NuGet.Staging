// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Stage.Authentication;

namespace Stage.Manager.Authentication
{
    public interface IAuthenticationCredentialsExtractor
    {
        ICredentials GetCredentials(HttpRequest request);
    }
}