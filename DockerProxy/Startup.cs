using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace DockerProxy
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        private void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.LiterateConsole(LogEventLevel.Debug,
                    "{Timestamp:HH:mm:ss.fff} [{ThreadId}] [{Level}] {Message}{NewLine}{Exception}");

            if (true)
                config.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Internal.WebHost", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Internal.ObjectResultExecutor", LogEventLevel.Warning);

            Log.Logger = config.CreateLogger();
            loggerFactory.AddSerilog();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvcCore();

            var builder = new ContainerBuilder();
            builder.Populate(services);

            var lifetime = (IApplicationLifetime)services.BuildServiceProvider().GetService(typeof(IApplicationLifetime));

            //Register proxy manager
            var proxyManager = new ProxyManager(lifetime.ApplicationStopping);
            builder
                .RegisterInstance(proxyManager)
                .AsSelf()
                .AsImplementedInterfaces();

            builder.RegisterInstance(Configuration)
                .AsSelf()
                .AsImplementedInterfaces();
            builder.RegisterAssemblyModules(GetType().GetTypeInfo().Assembly);

            var task = Task.Factory.StartNew(proxyManager.StartAsync, TaskCreationOptions.LongRunning);
            return new AutofacServiceProvider(builder.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            ConfigureLogging(loggerFactory);

            app.UseMvc();
        }
    }
}
