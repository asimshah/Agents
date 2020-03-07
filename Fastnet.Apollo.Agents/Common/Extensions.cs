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
            services.AddSingleton<Messenger>();
            services.AddSingleton<MusicServerListener>();
            foreach (var agent in agentConfiguration.Agents)
            {
                if (agent.Enabled)
                {
                    switch (agent.Name)
                    {
                        //case AgentName.MusicResampler:
                        //    EnsureResamplingConfiguration(services, configuration);
                        //    EnsureMusicDb(services, configuration);
                        //    services.AddSingleton<ScheduledTask, Resampler>();
                        //    break;
                        case AgentName.PortableMusicLibrary:
                            //EnsureResamplingConfiguration(services, configuration);
                            services.Configure<PortabilityConfiguration>(configuration.GetSection("PortabilityConfiguration"));
                            //EnsureMusicDb(services, configuration);
                            services.AddSingleton<ScheduledTask, MusicPortingTask>();
                            break;
                        case AgentName.MusicPlayer:
                            services.Configure<MusicPlayerOptions>(configuration.GetSection("MusicPlayerOptions"));
                            services.AddService<MusicPlayer>();
                            break;
                        case AgentName.MusicLibraryCopier:
                            //services.Configure<MusicLibraryCopyConfiguration>(configuration.GetSection("MusicLibraryCopyConfiguration"));
                            //services.AddSingleton<ScheduledTask, MusicLibraryCopier>();
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
