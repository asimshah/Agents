using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fastnet.Apollo.Agents
{
    public class Startup
    {
        //private readonly ILogger log;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            //this.log = logger;
            var version = typeof(Startup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Debug.WriteLine($"Agents {version.ToString()} site started");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(/*(x) => x.EnableDetailedErrors = true*/);
            services.AddOptions();
            services.Configure<MessengerOptions>(Configuration.GetSection("MessengerOptions"));
            services.Configure<AgentConfiguration>(Configuration.GetSection("AgentConfiguration"));
            services.Configure<SchedulerOptions>(Configuration.GetSection("SchedulerOptions"));
            services.AddAgentTasks(Configuration);

            // .net core 3.0's built-in json (System.Text.Json) is missing features as of Nov 2019:
            // 1. reference loop handling
            // 2. deserializing anonymous types
            // so I revert back to NewtonsoftJson here ....
            services.AddControllersWithViews()
                .AddNewtonsoftJson();

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
