// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Services.Staging.BackgroundWorkers;
using NuGet.Services.Staging.PackageService;
using Serilog;
using Serilog.Sinks.RollingFile;

namespace NuGet.Services.Staging.Runner
{
    public class Program
    {
        private const string _localEnvironmentName = "Local";

        private static IConfigurationRoot Configuration { get; set; }
        private static IServiceProvider ServiceProvider { get; set; }

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
            var worker = ServiceProvider.GetService<StageCommitWorker>();
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

            ConfigureDependencies(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            ConfigureLog(environment);
        }

        private static void ConfigureDependencies(IServiceCollection serviceCollection)
        {
            serviceCollection.Configure<TopicMessageListenerOptions>(Configuration.GetSection("TopicMessageListenerOptions"));
            serviceCollection.AddTransient<IMessageListener<PackageBatchPushData>, TopicMessageListener<PackageBatchPushData>>();
            serviceCollection.AddTransient<StageCommitWorker, StageCommitWorker>();
        }

        private static void ConfigureLog(string environment)
        {
            var loggerFactory = ServiceProvider.GetService<ILoggerFactory>();

          

            var serilogConfig = new LoggerConfiguration().MinimumLevel.Verbose();
            
            // Add application insights
            serilogConfig.WriteTo.ApplicationInsights(Configuration["ApplicationInsights:InstrumentationKey"]);

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

            Trace.AutoFlush = true;
            loggerFactory.AddTraceSource("StagingRunner", new TextWriterTraceListener("log.txt"));
        }

        private static void InitializeConfiguration(string environment)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.Combine("Config", $"config.{environment}.json"));

            Configuration = builder.Build();
        }

        private static bool IsLocalEnvironment(string environment)
        {
            return string.Compare(environment, _localEnvironmentName, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
