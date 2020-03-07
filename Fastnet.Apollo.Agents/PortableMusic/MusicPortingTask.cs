using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class MusicPortingTask : ScheduledTask
    {
        private MusicOptions musicOptions;
        private readonly MusicServerListener msl;
        //private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private readonly PortabilityConfiguration portabilityConfiguration;
        private readonly IWebHostEnvironment environment;
        public MusicPortingTask(ILoggerFactory loggerFactory, MusicServerListener msl,
            IConfiguration configuration, IWebHostEnvironment environment, /*IServiceProvider serviceProvider,*/
            IOptions<PortabilityConfiguration> portConfig/*, IOptions<ResamplingConfiguration> resampleConfig*/ ) : base(loggerFactory)
        {
            portabilityConfiguration = portConfig.Value;
            //resamplingOptions = resampleConfig;
            this.configuration = configuration;
            this.environment = environment;
            //this.serviceProvider = serviceProvider;
            this.msl = msl;
            BeforeTaskStartsAsync = async (m) => { await OnTaskStart(); };
        }

        //public override TimeSpan StartAfter => TimeSpan.Zero;
        private async Task OnTaskStart()
        {
            while (!this.msl.IsServerActive())
            {
                log.Information("waiting for music server ...");
                await Task.Delay(5000);
            }
            using (var msc = new MusicServerClient(this.msl.GetServerUrl(), this.loggerFactory.CreateLogger<MusicServerClient>()))
            {
                musicOptions = await msc.GetMusicOptions();
                //var styles = await msc.GetStyleInformation();
                await SetupPipeline(musicOptions.Styles);
            }
            //await SetupPipeline();
        }
        private Task SetupPipeline(StyleInformation[] styles)
        {
            List<IPipelineTask> list = new List<IPipelineTask>();
            foreach (var style in styles.Where(s => s.Enabled).Select(s => s.Style))
            {
                switch(style)
                {
                    case MusicStyles.Popular:
                        list.Add(new PortPopularMusic(musicOptions, configuration, environment, portabilityConfiguration));
                        break;
                    case MusicStyles.WesternClassical:
                        list.Add(new PortWesternClassicalMusic(musicOptions, configuration, environment, portabilityConfiguration/*, resamplingOptions*/));
                        break;
                }
                
            }

            CreatePipeline(list);
            return Task.FromResult<object>(null);
        }
    }
}
