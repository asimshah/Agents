using Fastnet.Agents.Server.Models;
using Fastnet.Agents.Server.Tasks;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Agents.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private IWebHostEnvironment environment;
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            this.environment = environment;
        }



        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var cs = Configuration.GetConnectionString("agentsConnection");
            Debug.Assert(cs != null && cs != string.Empty, $"connection string agentsConnection not found");
            cs = environment.LocaliseConnectionString(cs);
            services.AddSimpleAuthentication(Configuration, cs);
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddOptions();
            services.AddInitialiser<InitialiserService>();
            services.AddDbContext<AgentsDb>(options =>
            {
                options.UseSqlServer(cs)
                .UseLazyLoadingProxies();
            });
            services.AddScheduler(Configuration);
            services.AddSingleton<ScheduledTask, TestService>();
            services.AddSingleton<ScheduledTask, ScheduleBackupsService>();
            services.AddSingleton<ScheduledTask, BackupTask>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
