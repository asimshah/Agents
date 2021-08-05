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
        private static bool musicServerListenerEnabled = false;
        private static bool messengerEnabled = false;
        public static IServiceCollection AddAgentTasks(this IServiceCollection services, IConfiguration configuration/*, IHostingEnvironment environment*/)
        {
            var agentConfiguration = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(agentConfiguration);
            services.AddSingleton<AgentService>();
            foreach (var agent in agentConfiguration.Agents)
            {
                if (agent.Enabled)
                {
                    switch (agent.Name)
                    {
                        case AgentName.PortableMusicLibrary:
                            EnableMessenger(services);
                            EnableMusicServerListener(services);
                            services.Configure<PortabilityConfiguration>(configuration.GetSection("PortabilityConfiguration"));
                            services.AddSingleton<ScheduledTask, MusicPortingTask>();
                            break;
                        case AgentName.MusicPlayer:
                            EnableMessenger(services);
                            EnableMusicServerListener(services);
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
        private static void EnableMessenger(IServiceCollection services)
        {
            if(!messengerEnabled)
            {
                services.AddSingleton<Messenger>();
                messengerEnabled = true;
            }
        }
        private static void EnableMusicServerListener(IServiceCollection services)
        {
            if (!musicServerListenerEnabled)
            {
                services.AddSingleton<MusicServerListener>();
                musicServerListenerEnabled = true;
            }
        }
    }
}
