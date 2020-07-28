using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using NAudio.CoreAudioApi;
using NAudio.Wave;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fastnet.Apollo.Agents
{

    [Route("device")]
    public class DeviceController : BaseController
    {
        private readonly MusicPlayer musicPlayer;
        public DeviceController(/*MusicPlayer player,*/ IServiceProvider sp, IOptions<AgentConfiguration> agentcfg,
            ILogger<DeviceController> logger, IWebHostEnvironment env) : base(logger, env)
        {
            // *NB* cannot use Di for MusicPlayer because it may not be enabled in this instance's AgentConfiguration
            if(agentcfg.Value.Agents.Single(x => x.Name == AgentName.MusicPlayer).Enabled)
            {
                this.musicPlayer = sp.GetService<MusicPlayer>();
            }
            //this.musicPlayer = player;
        }
        [HttpPost("execute")]
        public async Task<IActionResult> Execute()
        {
            if (this.musicPlayer != null)
            {
                var pc = await this.Request.FromBody<PlayerCommand>();
                log.Information($"{pc}");
                await Task.Run(() => musicPlayer.ExecutePlayerCommand(pc));
            }
            //musicPlayer.ExecutePlayerCommand(pc);
            return SuccessResult();
        }
        [HttpGet("poll")]
        public IActionResult Poll()
        {
            log.Trace($"poll received");
            return SuccessResult();
        }

    }
}
