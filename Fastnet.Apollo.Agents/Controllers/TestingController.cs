using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fastnet.Apollo.Agents
{
    [Route("test")] 
    public class TestingController : BaseController
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly PortabilityConfiguration portabilityConfiguration;
        private readonly IConfiguration configuration;
        private readonly MusicServerListener msl;
        private readonly SchedulerService schedulerService;
        public TestingController(SchedulerService schedulerService, IWebHostEnvironment env, ILogger<TestingController> logger,
            MusicServerListener msl, IConfiguration configuration, IOptions<PortabilityConfiguration> portConfig, ILoggerFactory loggerFactory
            ) : base(logger, env)
        {
            this.schedulerService = schedulerService;
            this.msl = msl;
            this.configuration = configuration;
            portabilityConfiguration = portConfig.Value;
            this.loggerFactory = loggerFactory;
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
        [HttpGet("port/startIndianClassical")]
        public async Task<IActionResult> StartIndianClassical()
        {
            if(this.msl.IsServerActive())
            {
                using (var msc = new MusicServerClient(this.msl.GetServerUrl(), this.loggerFactory.CreateLogger<MusicServerClient>()))
                {
                    var musicOptions = await msc.GetMusicOptions();
                    var fn = new PortIndianClassicalMusicFN(musicOptions, configuration, environment, portabilityConfiguration);
                    await fn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                    var cn = new PortIndianClassicalMusicCN(musicOptions, configuration, environment, portabilityConfiguration);
                    await cn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                }
            }
            else
            {
                log.Error("Start music server first!!!");
            }

            return SuccessResult();
        }
        [HttpGet("port/startWesternClassical")]
        public async Task<IActionResult> StartWesternClassical()
        {
            if (this.msl.IsServerActive())
            {
                using (var msc = new MusicServerClient(this.msl.GetServerUrl(), this.loggerFactory.CreateLogger<MusicServerClient>()))
                {
                    var musicOptions = await msc.GetMusicOptions();
                    var fn = new PortWesternClassicalMusicFN(musicOptions, configuration, environment, portabilityConfiguration);
                    await fn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                    var cn = new PortWesternClassicalMusicCN(musicOptions, configuration, environment, portabilityConfiguration);
                    await cn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                }
            }
            else
            {
                log.Error("Start music server first!!!");
            }

            return SuccessResult();
        }
        [HttpGet("port/startPopular")]
        public async Task<IActionResult> StartPopular()
        {
            if (this.msl.IsServerActive())
            {
                using (var msc = new MusicServerClient(this.msl.GetServerUrl(), this.loggerFactory.CreateLogger<MusicServerClient>()))
                {
                    var musicOptions = await msc.GetMusicOptions();
                    var fn = new PortPopularMusicFN(musicOptions, configuration, environment, portabilityConfiguration);
                    await fn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                    var cn = new PortPopularMusicCN(musicOptions, configuration, environment, portabilityConfiguration);
                    await cn.ExecuteAsync.Invoke(null, ScheduleMode.OnRequest, new System.Threading.CancellationToken());
                }
            }
            else
            {
                log.Error("Start music server first!!!");
            }

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
