// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

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

    /// <summary>
    /// Useful links: Sql Azure retries: https://msdn.microsoft.com/en-us/library/dn440719(v=pandp.60).aspx
    /// </summary>
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

            return await RetryPolicy.DefaultFixed.ExecuteAsync(async () =>
            {
                using (var connection = new SqlConnection(_options.DatabaseConnectionString))
                {
                    using (var command = new SqlCommand(@"SELECT [UserKey]
                                                      FROM [dbo].[Credentials]
                                                      WHERE [Type]='apikey.v1' AND [Value]=@ApiKey", connection))
                    {
                        command.Parameters.AddWithValue("@ApiKey", credentials.ApiKey);

                        await connection.OpenAsync();
                        var userKey = await command.ExecuteScalarAsync();

                        return userKey == null ? null : new UserInformation {UserKey = (int) userKey};
                    }
                }
            });
        }

        public async Task<ApiKeyCredentials> GetCredentials(UserInformation userInformation)
        {
            if (userInformation == null)
            {
                return null;
            }

            return await RetryPolicy.DefaultFixed.ExecuteAsync(async () =>
            {
                using (var connection = new SqlConnection(_options.DatabaseConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"SELECT [Value]
                                                          FROM [dbo].[Credentials]
                                                          WHERE [UserKey]=@UserKey AND [Type]='apikey.v1'", connection))
                    {
                        command.Parameters.AddWithValue("@UserKey", userInformation.UserKey);

                        var userApiKey = await command.ExecuteScalarAsync();

                        return userApiKey == null ? null : new ApiKeyCredentials { ApiKey = (string)userApiKey };
                    }
                }
            });
        }
    }
}