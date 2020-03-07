using Fastnet.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class WasapiDevicePlayer : DevicePlayer
    {
        private MediaFoundationPlayer mediaFoundationPlayer;
        public WasapiDevicePlayer(AudioDevice device, MusicPlayerOptions mpo, Func<string, Task> requestNext, ILoggerFactory loggerFactory) : base(device, mpo, requestNext, loggerFactory)
        {
            //this.log = loggerFactory;
            mediaFoundationPlayer = new MediaFoundationPlayer(loggerFactory.CreateLogger<MediaFoundationPlayer>());
            var result = this.mediaFoundationPlayer.Initialise(device);
            if (result)
            {
                OnEvent(PlayerEvents.PlayerStarted);
            }
            else
            {
                OnEvent(PlayerEvents.FaultOccurred);
            }
            //SetState(result ? PlayerStates.Idle : PlayerStates.Fault);
        }
        //protected override Task<DeviceStatus> OnPulse()
        //{
        //    // read current state of device
        //    DeviceStatus ds = GetDeviceStatus();
        //    return Task.FromResult<DeviceStatus>(ds);
        //}
        protected override async Task Play(string url, float volume)
        {
            log.Debug($"request to call {url}, current isPlaying = isPlaying");
            mediaFoundationPlayer.PlayUrl(url, volume, () => {
                OnCurrentPlayStopped();
            });
            var ds = await GetDeviceStatus();
            this.onDeviceStatusUpdate(ds);
            return;// Task.CompletedTask;
        }
        // this method works in that it will download from the url and 
        // create a local file and I have only used iit to test the idea
        // a download of a 145Mbyte file to about 28 seconds (all within the same computer)
        // Not sure if in fact I wil ever use this method - lets see
        // how ell things go across the LAN first
        //private async Task<string> DownloadMusic(string url)
        //{
        //    try
        //    {
        //        var filename = Path.Combine("c:\\", "temp", "f1.flac");
        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        using (var cl1 = new WebClient())
        //        {
        //            await cl1.DownloadFileTaskAsync(url, filename);
        //        }
        //        sw.Stop();
        //        log.Information($"downloaded in {sw.ElapsedMilliseconds}");
        //        return filename;
        //    }
        //    catch (Exception)
        //    {
        //        Debugger.Break();
        //        throw;
        //    }
        //}
        protected override void OnCancel()
        {
            mediaFoundationPlayer?.Dispose();
            //mediaFoundationPlayer?.Stop();
            //mediaFoundationPlayer = null;
        }
        protected override async Task<bool> TogglePlayPause()
        {
            var r = mediaFoundationPlayer?.TogglePlayPause() ?? false;
            var ds = await GetDeviceStatus();
            this.onDeviceStatusUpdate(ds);
            return r;// Task.FromResult(r);
        }
        protected override async Task Reposition(float position)
        {
            mediaFoundationPlayer?.Reposition(position);
            var ds = await GetDeviceStatus();
            this.onDeviceStatusUpdate(ds);
            return;// Task.CompletedTask;
        }
        protected override async Task SetVolume(float level)
        {
            mediaFoundationPlayer?.SetVolume(level);
            var ds = await GetDeviceStatus();
            this.onDeviceStatusUpdate(ds);
            return;// Task.CompletedTask;
        }
        protected override Task<DeviceStatus> GetDeviceStatus()
        {
            (var playbackState, var volume, var currentTime, var totalTime) = this.mediaFoundationPlayer.GetStatusInformation();
            var ds = new DeviceStatus
            {
                Key = device.Key,
                State = GetState(),
                Volume = volume,
                CurrentTime = currentTime,
                TotalTime = totalTime//,
                //IsPaused = mediaFoundationPlayer.GetPlayerStatus() == PlayerStatus.IsPaused
            };
            return Task.FromResult<DeviceStatus>(ds);
        }

        protected override Task StopPlaying()
        {
            mediaFoundationPlayer?.Stop();
            return Task.CompletedTask;
        }
    }
}
