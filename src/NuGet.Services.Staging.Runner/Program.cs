// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Services.Logging;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.Common;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;
using Serilog.Events;
using Serilog.Sinks.RollingFile;

namespace NuGet.Services.Staging.Runner
{
    public class Program
    {
        private const string _localEnvironmentName = "Local";

        private static IConfigurationRoot _configuration { get; set; }
        private static IServiceProvider _serviceProvider { get; set; }

        public static void Main(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                throw new ArgumentException("Environment not specified! Options: local, development, int, production");
            }

            Startup(args[0]);

            Run();
        }

        private static void Run()
        {
            var worker = _serviceProvider.GetService<StageCommitWorker>();
            worker.Start();

            while (worker.IsActive)
            {
                Trace.WriteLine("Working...");
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }

            Trace.WriteLine("Worker no longer active. Exiting.");
        }

        private static void Startup(string environment)
        {
            InitializeConfiguration(environment);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();

            ConfigureLog(environment, serviceCollection);

            serviceCollection.AddEntityFramework().AddEntityFrameworkSqlServer();


            ConfigureDependencies(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureDependencies(IServiceCollection serviceCollection)
        {
            serviceCollection.Configure<TopicMessageListenerOptions>(_configuration.GetSection("TopicMessageListenerOptions"));
            serviceCollection.AddTransient<IMessageListener<PackageBatchPushData>, TopicMessageListener<PackageBatchPushData>>();

            serviceCollection.AddTransient<StageCommitWorker, StageCommitWorker>();
            serviceCollection.AddTransient<ICommitStatusService, CommitStatusService>();
            serviceCollection.AddTransient<IReadOnlyStorage, AzureReadOnlyStorage>();
            serviceCollection.AddTransient<IPackageMetadataService, PackageMetadataService>();

            serviceCollection.AddTransient<IPackagePushService, PackagePushService>();
            serviceCollection.Configure<PackagePushServiceOptions>(_configuration.GetSection("PackagePushServiceOptions"));

            serviceCollection.Configure<ApiKeyAuthenticationServiceOptions>(_configuration.GetSection("ApiKeyAuthenticationServiceOptions"));
            serviceCollection.AddSingleton<ApiKeyAuthenticationService, ApiKeyAuthenticationService>();

            serviceCollection.AddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();
            serviceCollection.AddTransient<IMessageHandler<PackageBatchPushData>, BatchPushHandler>();

            // Configure StageContext explicitly, instead of using extension method AddDBContext,
            // since the extension method adds the context as scoped, and we need it to be Transient
            var connectionString = _configuration["StageDatabase:ConnectionString"];
            var optionsBuilder = new DbContextOptionsBuilder<StageContext>();
            optionsBuilder.UseSqlServer(connectionString);
            serviceCollection.AddSingleton<DbContextOptions<StageContext>>(_ => optionsBuilder.Options);
            serviceCollection.AddSingleton<DbContextOptions>(p => p.GetRequiredService<DbContextOptions<StageContext>>());
            serviceCollection.AddTransient<StageContext, StageContext>();
        }

        private static void ConfigureLog(string environment, IServiceCollection serviceCollection)
        {
            var loggingConfig = LoggingSetup.CreateDefaultLoggerConfiguration(withConsoleLogger: IsLocalEnvironment(environment));

            // Write to file
            loggingConfig.WriteTo.RollingFile("StageRunnerLog-{Date}.txt");

            // Write to AI
            ApplicationInsights.Initialize(_configuration["ApplicationInsights:InstrumentationKey"]);

            var loggerFactory = LoggingSetup.CreateLoggerFactory(loggingConfig, LogEventLevel.Verbose);
            serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
        }

        private static void InitializeConfiguration(string environment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.Combine("Config", $"config.{environment}.json")).Build();

            _configuration = new KeyVaultConfigurationReader(builder, new SecretReaderFactory(builder));
        }

        private static bool IsLocalEnvironment(string environment)
        {
            return string.Compare(environment, _localEnvironmentName, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
