// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.PackageService;
using Serilog;
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
            serviceCollection.AddLogging();

            var connectionString = _configuration["StageDatabase:ConnectionString"];

            serviceCollection.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<StageContext>(options => options.UseSqlServer(connectionString));

            ConfigureDependencies(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            ConfigureLog(environment);
        }

        private static void ConfigureDependencies(IServiceCollection serviceCollection)
        {
            serviceCollection.Configure<TopicMessageListenerOptions>(_configuration.GetSection("TopicMessageListenerOptions"));
            serviceCollection.AddTransient<IMessageListener<PackageBatchPushData>, TopicMessageListener<PackageBatchPushData>>();
            serviceCollection.AddTransient<StageCommitWorker, StageCommitWorker>();
            serviceCollection.AddTransient<StageContext, StageContext>();
        }

        private static void ConfigureLog(string environment)
        {
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();

            var serilogConfig = new LoggerConfiguration().MinimumLevel.Verbose();
            
            // Add application insights
            serilogConfig.WriteTo.ApplicationInsights(_configuration["ApplicationInsights:InstrumentationKey"]);

            // Hook into anything that is being traced in other libs using system.diagnostics.trace
            Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

            // Write to file
            serilogConfig.WriteTo.RollingFile("StageRunnerLog-{Date}.txt");

            if (IsLocalEnvironment(environment))
            {
                loggerFactory.AddDebug();
                serilogConfig.WriteTo.Console();
            }

            Log.Logger = serilogConfig.CreateLogger();
            loggerFactory.AddSerilog();
        }

        private static void InitializeConfiguration(string environment)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.Combine("Config", $"config.{environment}.json"));

            _configuration = builder.Build();
        }

        private static bool IsLocalEnvironment(string environment)
        {
            return string.Compare(environment, _localEnvironmentName, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
