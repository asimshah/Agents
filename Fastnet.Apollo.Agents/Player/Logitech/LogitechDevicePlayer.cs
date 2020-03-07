using Fastnet.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class LogitechDevicePlayer : DevicePlayer
    {
        private bool currentlyPlaying; // true, even if paused
        private readonly LMSClient lmc;
        public LogitechDevicePlayer(AudioDevice device, MusicPlayerOptions mpo, Func<string, Task> requestNext, ILoggerFactory loggerFactory) : base(device, mpo, requestNext, loggerFactory)
        {
            lmc = new LMSClient(this.musicPlayerOptions, this.lf.CreateLogger<LMSClient>());
            var lps = lmc.PlayerInformation(device.MACAddress).Result;
            if (lps != null)
            {
                if (lps.Mode != "stop")
                {
                    Task.Run(async () => {
                        await StopPlaying();
                        OnEvent(PlayerEvents.PlayerStarted);
                        //await lmc.Stop(device.MACAddress);
                    });
                }
                else
                {
                    OnEvent(PlayerEvents.PlayerStarted);
                }

            }
            else
            {
                OnEvent(PlayerEvents.FaultOccurred);
            }
        }

        protected override async Task<DeviceStatus> GetDeviceStatus()
        {
            return await GetDeviceStatusInternal();
            //var ds = new DeviceStatus();
            //var lps = await lmc.PlayerInformation(device.MACAddress);
            //if (lps != null)
            //{
            //    ds.Key = this.device.Key;
            //    ds.Volume = lps.Volume / 100.0f;
            //    ds.CurrentTime = TimeSpan.FromSeconds(lps.Position);
            //    ds.TotalTime = TimeSpan.FromSeconds(lps.Duration);
            //    switch (lps.Mode)
            //    {
            //        case "play":
            //            ds.State = Music.Core.PlayerStates.Playing;
            //            break;
            //        case "pause":
            //            ds.State = Music.Core.PlayerStates.Paused;
            //            break;
            //        case "stop":
            //            if(currentlyPlaying)
            //            {
            //                ds.State = GetState();// Music.Core.PlayerStates.Playing;
            //                currentlyPlaying = false;
            //                OnCurrentPlayStopped();
            //            }
            //            else
            //            {
            //                ds.State = GetState();
            //            }
            //            break;
            //    }
            //}
            //else
            //{
            //    ds.State = Music.Core.PlayerStates.Fault;
            //    log.Error($"Logitech {device.DisplayName} not responding");
            //}
            //return ds;
        }
        private async Task<DeviceStatus> GetDeviceStatusInternal(LogitechDeviceStatus lps = null)
        {
            var ds = new DeviceStatus();
            if (lps == null)
            {
                lps = await lmc.PlayerInformation(device.MACAddress);
            }
            if (lps != null)
            {
                ds.Key = this.device.Key;
                ds.Volume = lps.Volume / 100.0f;
                ds.CurrentTime = TimeSpan.FromSeconds(lps.Position);
                ds.TotalTime = TimeSpan.FromSeconds(lps.Duration);
                switch (lps.Mode)
                {
                    case "play":
                        ds.State = Music.Core.PlayerStates.Playing;
                        break;
                    case "pause":
                        ds.State = Music.Core.PlayerStates.Paused;
                        break;
                    case "stop":
                        if (currentlyPlaying)
                        {
                            ds.State = GetState();// Music.Core.PlayerStates.Playing;
                            currentlyPlaying = false;
                            OnCurrentPlayStopped();
                        }
                        else
                        {
                            ds.State = GetState();
                        }
                        break;
                }
            }
            else
            {
                ds.State = Music.Core.PlayerStates.Fault;
                log.Error($"Logitech {device.DisplayName} not responding");
            }
            return ds;
        }
        protected override void OnCancel()
        {
            lmc?.Dispose();
        }

        protected override async Task Play(string url, float volume)
        {
            currentlyPlaying = true;
            await lmc.Play(device.MACAddress, url);
            await SetVolume(volume);
        }

        protected override async Task Reposition(float position)
        {
            var lps = await lmc.PlayerInformation(device.MACAddress);
            if (lps != null)
            {
                var required = lps.Duration * position;
                await lmc.JumpTo(device.MACAddress, required);
                var ds = await GetDeviceStatus();
                this.onDeviceStatusUpdate(ds);
            }
        }

        protected override async Task SetVolume(float level)
        {
            log.Information($"set volume to level {level}");
            await lmc.SetVolume(device.MACAddress, level * 100);
            var ds = await GetDeviceStatus();
            this.onDeviceStatusUpdate(ds);
        }

        protected override async Task<bool> TogglePlayPause()
        {
            var lps = await lmc.PlayerInformation(device.MACAddress);
            if (lps != null)
            {
                switch (lps.Mode)
                {
                    case "play":
                        await lmc.Pause(device.MACAddress);
                        break;
                    case "pause":
                        await lmc.Resume(device.MACAddress);
                        break;
                    case "stop":
                        log.Warning($"Logitech {device.DisplayName} responds in state 'stop'");
                        break;
                }
                lps = await lmc.PlayerInformation(device.MACAddress);
                var ds = await GetDeviceStatusInternal(lps);
                this.onDeviceStatusUpdate(ds);
                return lps.Mode == "play";
            }
            return false;
        }

        protected override async Task StopPlaying()
        {
            await lmc.Stop(device.MACAddress);
        }
    }
}
