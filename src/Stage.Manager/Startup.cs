﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.V3Repository;
using Stage.Database.Models;
using Stage.Manager.Logging;
using Stage.Packages;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace Stage.Manager
{
    /// <summary>
    /// Helpful links:
    /// https://github.com/Microsoft/ApplicationInsights-aspnetcore/wiki/Getting-Started
    /// https://docs.asp.net/en/latest/fundamentals/startup.html
    /// </summary>
    public class Startup
    {
        private const string _localEnvironmentName = "Local";

        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment hostingEnvironment, IApplicationEnvironment applicationEnvironment)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(applicationEnvironment.ApplicationBasePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.Combine("Config", $"config.{hostingEnvironment.EnvironmentName}.json") )
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            if (hostingEnvironment.IsEnvironment(_localEnvironmentName))
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();

            var connectionString = Configuration["StageDatabase:ConnectionString"];

            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<StageContext>(options => options.UseSqlServer(connectionString));

            ConfigureDependencies(services);
        }

        private void ConfigureDependencies(IServiceCollection services)
        {
            // IPackageService setup
            services.AddScoped<IPackageService, DatabasePackageService>();
            services.Configure<DatabasePackageServiceOptions>(Configuration.GetSection("DatabasePackageServiceOptions"));

            services.AddScoped<IStageService, StageService>();

            // V3
            services.Configure<V3ServiceOptions>(options =>
            {
                options.CatalogFolderName = Constants.CatalogFolderName;
                options.FlatContainerFolderName = Constants.FlatContainerFolderName;
                options.RegistrationFolderName = Constants.RegistrationFolderName;
            });

            services.AddSingleton<IV3ServiceFactory, V3ServiceFactory>();

            string storageAccountConnectionString = Configuration["PackageRepository:StorageAccountConnectionString"];
            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountConnectionString);
            services.AddInstance<StorageFactory>(new AzureStorageFactory(account, Constants.StagesContainerName));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder applicationBuilder, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory)
        {
            if (hostingEnvironment.IsEnvironment(_localEnvironmentName))
            {
                loggerFactory.AddConsole(Configuration.GetSection("Logging"));
                loggerFactory.AddDebug();
            }

            // Debug settings for local runs and for dev deployment
            if (hostingEnvironment.IsEnvironment(_localEnvironmentName) || hostingEnvironment.IsDevelopment())
            {
                applicationBuilder.UseDeveloperExceptionPage();
            }

            applicationBuilder.UseIISPlatformHandler();

            // Add Application Insights monitoring to the request pipeline as a very first middleware.
            applicationBuilder.UseApplicationInsightsRequestTelemetry();

            // Add Application Insights exceptions handling to the request pipeline.
            // Exception middleware should be added after error page and any other error handling middleware
            applicationBuilder.UseApplicationInsightsExceptionTelemetry();

            applicationBuilder.UseMvc();

            loggerFactory.AddApplicationInsights(applicationBuilder.ApplicationServices.GetService<TelemetryClient>());
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
