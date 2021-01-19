using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fastnet.Apollo.Agents
{
    [Route("test")] 
    public class TestingController : BaseController
    {
        private readonly MessengerOptions messengerOptions;
        private readonly SchedulerService schedulerService;
        public TestingController(IOptions<MessengerOptions> mo, SchedulerService schedulerService, IWebHostEnvironment env, ILogger<TestingController> logger): base(logger, env)
        {
            this.schedulerService = schedulerService;
            this.messengerOptions = mo.Value;
        }
        [HttpGet("port/start")]
        public async Task<IActionResult> StartMusicPort()
        {
            await schedulerService.ExecuteNow<MusicPortingTask>();
            return SuccessResult();
        }
        [HttpGet("backup/start")]
        public async Task<IActionResult> StartBackup()
        {
            await schedulerService.ExecuteNow<BackupTask>();
            return SuccessResult();
        }
        [HttpGet("webbackup/start")]
        public async Task<IActionResult> StartWbDbBackup()
        {
            await schedulerService.ExecuteNow<WebDbBackupTask>();
            return SuccessResult();
        }
        [HttpGet("options/1")]
        public async Task<IActionResult> Options1()
        {
            await Task.Delay(0);
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
