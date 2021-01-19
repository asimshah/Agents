using Fastnet.Apollo.Agents.Services;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public static class Extensions
    {
        public static IServiceCollection AddAgentTasks(this IServiceCollection services, IConfiguration configuration/*, IHostingEnvironment environment*/)
        {
            var agentConfiguration = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(agentConfiguration);
            services.AddSingleton<AgentService>();
            services.AddSingleton<Messenger>();
            services.AddSingleton<MusicServerListener>();
            foreach (var agent in agentConfiguration.Agents)
            {
                if (agent.Enabled)
                {
                    switch (agent.Name)
                    {
                        case AgentName.PortableMusicLibrary:
                            services.Configure<PortabilityConfiguration>(configuration.GetSection("PortabilityConfiguration"));
                            services.AddSingleton<ScheduledTask, MusicPortingTask>();
                            break;
                        case AgentName.MusicPlayer:
                            services.Configure<MusicPlayerOptions>(configuration.GetSection("MusicPlayerOptions"));
                            services.AddService<MusicPlayer>();
                            break;
                        //case AgentName.MusicLibraryCopier:
                        //    break;
                        case AgentName.FolderBackup:
                            services.Configure<BackupConfiguration>(configuration.GetSection("BackupConfiguration"));
                            services.AddSingleton<ScheduledTask, BackupTask>();
                            break;
                        case AgentName.WebDatabaseBackup:
                            services.Configure<WebDbBackupConfiguration>(configuration.GetSection("WebDbBackupConfiguration"));
                            services.AddSingleton<ScheduledTask, WebDbBackupTask>();
                            break;
                        //case AgentName.ContactSynchroniser:
                        //    services.Configure<ContactSynchroniserConfiguration>(configuration.GetSection("ContactSynchroniserConfiguration"));
                        //    services.AddSingleton<ScheduledTask, ContactSynchroniser>();
                        //    services.AddDbContext<ContactsDb>(options =>
                        //    {
                        //        var cs = environment.LocaliseConnectionString(configuration.GetConnectionString("ContactsDb"));
                        //        options.UseSqlServer(cs)
                        //            .UseLazyLoadingProxies();
                        //    });
                        //    break;
                    }
                }
            }
            services.AddScheduler(configuration);
            return services;
        }
    }
}
