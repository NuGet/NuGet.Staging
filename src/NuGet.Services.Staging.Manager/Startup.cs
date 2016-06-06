// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Logging;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Staging.Authentication;
using NuGet.Services.Staging.Database.Models;
using NuGet.Services.Staging.Manager.Authentication;
using NuGet.Services.Staging.Manager.V3;
using NuGet.Services.Staging.PackageService;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace NuGet.Services.Staging.Manager
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

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.Combine("Config", $"config.{hostingEnvironment.EnvironmentName}.json"))
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
                .AddEntityFrameworkSqlServer()
                .AddDbContext<StageContext>(options => options.UseSqlServer(connectionString, b => b.MigrationsAssembly("NuGet.Services.Staging.Manager")));

            ConfigureLogging(services);
            ConfigureDependencies(services);
        }

        private void ConfigureDependencies(IServiceCollection services)
        {
            // IPackageService setup
            services.AddScoped<IPackageService, InternalPackageService>();
            services.Configure<InternalPackageServiceOptions>(Configuration.GetSection("InternalPackageServiceOptions"));
            
            // StageService
            services.AddScoped<IStageService, StageService>();

            // V3
            services.AddSingleton<IV3ServiceFactory, V3ServiceFactory>();
            services.AddSingleton<StageIndexBuilder, StageIndexBuilder>();

            string storageAccountConnectionString = Configuration["PackageRepository:StorageAccountConnectionString"];
            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountConnectionString);
            services.AddSingleton<StorageFactory>(new AzureStorageFactory(account, Constants.StagesContainerName));

            // Search
            services.AddScoped<ISearchServiceFactory, SearchServiceFactory>();

            // Authentication
            services.Configure<ApiKeyAuthenticationServiceOptions>(Configuration.GetSection("ApiKeyAuthenticationServiceOptions"));
            services.AddSingleton<ApiKeyAuthenticationService, ApiKeyAuthenticationService>();

            // Filters
            services.AddScoped<EnsureStageExistsFilter, EnsureStageExistsFilter>();
            services.AddScoped<EnsureUserIsOwnerOfStageFilter, EnsureUserIsOwnerOfStageFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder applicationBuilder, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory)
        {
            // Debug settings for local runs and for dev deployment
            if (hostingEnvironment.IsEnvironment(_localEnvironmentName) || hostingEnvironment.IsDevelopment())
            {
                applicationBuilder.UseDeveloperExceptionPage();
            }

            // Add Application Insights monitoring to the request pipeline as a very first middleware.
            applicationBuilder.UseApplicationInsightsRequestTelemetry();

            // Add Application Insights exceptions handling to the request pipeline.
            // Exception middleware should be added after error page and any other error handling middleware
            applicationBuilder.UseApplicationInsightsExceptionTelemetry();

            applicationBuilder.UseApiKeyAuthentication();
            applicationBuilder.UseMvc();
        }

        private void ConfigureLogging(IServiceCollection serviceCollection)
        {
            var loggingConfig = LoggingSetup.CreateDefaultLoggerConfiguration();

            // Add application insights
            ApplicationInsights.Initialize(Configuration["ApplicationInsights:InstrumentationKey"]);

            var loggerFactory = LoggingSetup.CreateLoggerFactory(loggingConfig);
            serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables("ASPNETCORE_").Build();

            var host = new WebHostBuilder()
                        .UseConfiguration(configuration)
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseStartup<Startup>()
                        .Build();

            host.Run();
        }
    }
}