using System.Threading.Tasks;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fastnet.Apollo.Agents
{
    [Route("port")] 
    public class PortingController : BaseController
    {
        private readonly SchedulerService schedulerService;
        public PortingController(SchedulerService schedulerService, IWebHostEnvironment env, ILogger<PortingController> logger): base(logger, env)
        {
            this.schedulerService = schedulerService;
        }
        [HttpGet("start")]
        public async Task<IActionResult> StartMusicPort()
        {
            await schedulerService.ExecuteNow<MusicPortingTask>();
            return SuccessResult();
        }
        //[HttpGet("naudio")]
        //public IActionResult TestNaudio()
        //{
        //    var reader = new Mp3FileReader(@"D:\Music Staging\mp3\Western\Popular\Berlin\(Berlin) - Take My Breath Away.mp3");
        //    var waveOut = new WaveOutEvent();
        //    waveOut.Init(reader);
        //    waveOut.Play();
        //    return null;
        //}
    }
}
