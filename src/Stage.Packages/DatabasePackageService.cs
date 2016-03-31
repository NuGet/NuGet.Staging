﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.OptionsModel;

namespace Stage.Packages
{
    public class DatabasePackageServiceOptions
    {
        public string DatabaseConnectionString { get; set; }
    }

    public class DatabasePackageService : IPackageService
    {
        private readonly DatabasePackageServiceOptions _options;

        public DatabasePackageService(IOptions<DatabasePackageServiceOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        public async Task<bool> DoesPackageExistsAsync(string id, string version)
        {
            using (var connection = new SqlConnection(_options.DatabaseConnectionString))
            {
                using (var command = new SqlCommand(@"SELECT COUNT([dbo].[Packages].[Key])
                                               FROM [dbo].[PackageRegistrations]
                                               INNER JOIN [dbo].[Packages]
                                               ON  [dbo].[PackageRegistrations].[Key] = [dbo].[Packages].[PackageRegistrationKey]
                                               WHERE [dbo].[PackageRegistrations].[Id]=@Id AND [dbo].[Packages].[NormalizedVersion]=@Version", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Version", version);

                    await connection.OpenAsync();
                    var scalar = await command.ExecuteScalarAsync();

                    return (int) scalar != 0;
                }
            }
        }

        /// <summary>
        /// Checks if the user is an owner of the registration id. If the registration id doesn't exist, true is returned.
        /// </summary>
        public async Task<bool> IsUserOwnerOfPackageAsync(int userKey, string id)
        {
            using (var connection = new SqlConnection(_options.DatabaseConnectionString))
            {
                int registrationKey;

                using (var command = new SqlCommand(@"SELECT [dbo].[PackageRegistrations].[Key]
                                               FROM [dbo].[PackageRegistrations]
                                               WHERE [dbo].[PackageRegistrations].[Id]=@Id", connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@Id", id);

                    await connection.OpenAsync();
                    var scalar = await command.ExecuteScalarAsync();

                    // No such registration
                    if (scalar == null)
                    {
                        return true;
                    }

                    registrationKey = (int)scalar;
                } 

                using (var command = new SqlCommand(@"SELECT [dbo].[PackageRegistrationOwners].[UserKey]
                                               FROM [dbo].[PackageRegistrationOwners]
                                               WHERE [dbo].[PackageRegistrationOwners].[PackageRegistrationKey]=@Key AND [dbo].[PackageRegistrationOwners].[UserKey]=@UserKey", connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@Key", registrationKey);
                    command.Parameters.AddWithValue("@UserKey", userKey);

                    var scalar = await command.ExecuteScalarAsync();

                    return scalar != null;
                }
            }
        }
    }
}
