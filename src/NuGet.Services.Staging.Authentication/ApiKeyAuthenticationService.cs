// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.OptionsModel;

namespace NuGet.Services.Staging.Authentication
{
    public class ApiKeyCredentials
    {
        public string ApiKey { get; set; }
    }

    public class ApiKeyAuthenticationServiceOptions
    {
        public string DatabaseConnectionString { get; set; }
    }

    public class ApiKeyAuthenticationService
    {
        private readonly ApiKeyAuthenticationServiceOptions _options;

        public ApiKeyAuthenticationService(IOptions<ApiKeyAuthenticationServiceOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        public async Task<UserInformation> Authenticate(ApiKeyCredentials credentials)
        {
            if (credentials == null)
            {
                return null;
            }

            using (var connection = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var command = new SqlCommand(@"SELECT [Key]
                                                      FROM [dbo].[Users]
                                                      WHERE [ApiKey]=@ApiKey", connection))
                {
                    command.Parameters.AddWithValue("@ApiKey", ((ApiKeyCredentials)credentials).ApiKey);

                    await connection.OpenAsync();
                    var userKey = await command.ExecuteScalarAsync();

                    return userKey == null ? null : new UserInformation { UserKey = (int)userKey };
                }
            }
        }
    }
}