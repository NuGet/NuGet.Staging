// Copyright (c) .NET Foundation. All rights reserved.
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
using Stage.Database.Models;
using Stage.Manager.Logging;
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
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();

            var connectionString = Configuration["StageDatabase:ConnectionString"];

            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<StageContext>(options => options.UseSqlServer(connectionString));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder applicationBuilder, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            // Debug settings for local runs and for dev deployment
            if (hostingEnvironment.IsEnvironment(_localEnvironmentName) || hostingEnvironment.IsDevelopment())
            {
                loggerFactory.AddDebug();
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
