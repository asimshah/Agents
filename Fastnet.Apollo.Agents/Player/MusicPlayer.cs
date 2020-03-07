using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class MusicPlayer : HostedService
    {
        public class DevicePlayerTask
        {
            public Task Task { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public DevicePlayer DevicePlayer { get; set; }
            public PlayerCommand MostRecentCommand { get; set; }
        }
        private object sentinel = new object();
        /// <summary>
        /// this is a list of local audio devices that are enabled in the configuration (which enables by device type: wasapi, or logitech etc)
        /// and in the music server database
        /// </summary>
        private ConcurrentDictionary<string, (AudioDevice audioDevice, DevicePlayerTask devicePlayerTask)> audioDevices;
        private FindLocalAudioDevices deviceUpdater;
        private CancellationToken musicPlayerCancellationToken;
        private readonly Messenger messenger;
        private readonly MusicPlayerOptions musicPlayerOptions;
        private readonly MessengerOptions messengerOptions;
        private readonly ILoggerFactory lf;
        //private readonly List<Task> taskList;
        //private readonly List<IPausableTask> pausables;
        private readonly MusicServerListener msl;
        private IWebHostEnvironment environment;
        public MusicPlayer(MusicServerListener msl, Messenger messenger, IOptions<MessengerOptions> messOptions,
            IOptions<MusicPlayerOptions> mpo, IWebHostEnvironment webHostEnvironment,
            ILogger<MusicPlayer> log, ILoggerFactory loggerFactory) : base(log)
        {
            this.messengerOptions = messOptions.Value;
            this.messenger = messenger;
            this.msl = msl;
            this.messenger.EnableMulticastSend();
            this.musicPlayerOptions = mpo.Value;
            this.lf = loggerFactory;
            this.environment = webHostEnvironment;
            audioDevices = new ConcurrentDictionary<string, (AudioDevice audioDevice, DevicePlayerTask devicePlayerTask)>();
            this.msl.StateChanged += (tf) => OnServerStateChanged(tf);
            this.msl.DeviceNameChanged += (d) => OnDeviceNameChanged(d);
            this.msl.DeviceEnabled += (d) => OnDeviceEnabled(d);
            this.msl.DeviceDisabled += (d) => OnDeviceDisabledAsync(d);
            //this.msl = new MusicServerListener(lf.CreateLogger<MusicServerListener>(), musicPlayerOptions.Intervals.ServerInformationBroadcastInterval,
            //    (tf) => OnServerStateChanged(tf),
            //    (d) => OnDeviceNameChanged(d), (d) => OnDeviceEnabled(d), async (d) => await OnDeviceDisabledAsync(d)
            //    );
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            musicPlayerCancellationToken = cancellationToken;
            //this.msl.Start();
            //messenger.AddMulticastSubscription<MusicServerInformation>(async (m) => { await this.msl.OnMusicServerInformation(m); });
            await StartFindLocalAudioDevicesTask();
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                if (musicPlayerCancellationToken.IsCancellationRequested)
                {
                    log.Information($"cancellation requested, shutting down");
                    break;
                }

            };
            //await Task.WhenAll(taskList);
        }
        public void ExecutePlayerCommand(PlayerCommand pc)
        {
            if (audioDevices.ContainsKey(pc.DeviceKey))
            {
                (var device, var dpt) = audioDevices[pc.DeviceKey];
                switch (pc.Command)
                {
                    default:
                        log.Warning($"Player Command {pc.Command.ToString()} not implemented");
                        break;
                    case PlayerCommands.SetVolume:// .JumpTo:
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.SetVolume, pc.Volume);
                        break;
                    case PlayerCommands.SetPosition:// .JumpTo:
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.Reposition, pc.Position);
                        break;
                    case PlayerCommands.TogglePlayPause:
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.TogglePlayPause);
                        break;
                    case PlayerCommands.Play: //.ClearThenPlay: // ?? should be just called Play ??
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.Play, $"{this.msl.GetServerUrl()}/{pc.StreamUrl}", pc.Volume);
                        break;
                    case PlayerCommands.ListFinished: //.Stop: // // list finished??
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.ListFinished);
                        break;
                    case PlayerCommands.Reset: //.Stop: // // list finished??
                        dpt.MostRecentCommand = pc;
                        dpt.DevicePlayer.OnEvent(PlayerEvents.Reset);
                        break;
                }

            }
        }
        public void LogStatus()
        {
            log.Information($"{audioDevices.Count()} current audio devices:");
            foreach (var item in audioDevices)
            {
                (var audioDevice, var devicePlayerTask) = item.Value;
                log.Information($"   {audioDevice.DisplayName ?? "(no display name)" }: player is {devicePlayerTask?.DevicePlayer?.ToString() ?? "(no device player)"}, last command {devicePlayerTask.MostRecentCommand?.ToString() ?? "(none)"}");
            }
        }
        private Task OnServerStateChanged(bool connected)
        {
            log.Information($"server state changed: connected = {connected}");
            if (connected)
            {

            }
            return Task.CompletedTask;
        }
        private Task OnDeviceNameChanged(AudioDevice d)
        {
            if (this.audioDevices.ContainsKey(d.Key))
            {
                audioDevices[d.Key].audioDevice.DisplayName = d.DisplayName;
            }
            return Task.CompletedTask;
        }
        private Task OnDeviceEnabled(AudioDevice d)
        {
            if (!this.audioDevices.ContainsKey(d.Key))
            {
                if (d.Type != AudioDeviceType.Browser)
                {
                    AddAudioDevice(d);
                }
            }
            return Task.CompletedTask;
        }
        private async Task OnDeviceDisabledAsync(AudioDevice d)
        {
            if (this.audioDevices.ContainsKey(d.Key))
            {
                await RemoveDevice(d.Key);
            }
        }
        private DevicePlayerTask StartDevicePlayer(AudioDevice d)
        {
            var dp = DevicePlayer.GetDevicePlayer(d, this.musicPlayerOptions, OnRequestNext, this.lf);
            if (dp != null)
            {
                var dpt = new DevicePlayerTask();
                dpt.CancellationTokenSource = new CancellationTokenSource();
                dpt.DevicePlayer = dp;
                Task.Run(() =>
                {
                    dpt.Task = dpt.DevicePlayer.Start(async (ds) => await OnDeviceStatusUpdate(ds), dpt.CancellationTokenSource.Token);
                });
                return dpt;
            }
            else
            {
                log.Warning($"No device player implemented for device type {d.Type.ToString()}");
            }
            return null;
        }
        private async Task OnRequestNext(string deviceKey)
        {
            if (this.msl.IsServerActive())
            {
                using (var msc = new MusicServerClient(this.msl.GetServerUrl(), lf.CreateLogger<MusicServerClient>()))
                {
                    try
                    {
                        await msc.GetNextPlaylistItem(deviceKey);
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe);
                    }
                }
            }
        }
        private async Task OnDeviceStatusUpdate(DeviceStatus ds)
        {
            try
            {
                if (this.msl.IsServerActive())
                {
                    if (this.audioDevices.ContainsKey(ds.Key))
                    {
                        (var device, var dpt) = this.audioDevices[ds.Key];

                        switch (ds.State)
                        {
                            case PlayerStates.Fault:
                            case PlayerStates.Initial:
                            case PlayerStates.SilentIdle:
                                // no device status broadcast in these states
                                break;
                            default:
                                await messenger.SendMulticastAsync(ds);
                                break;
                        }
                    }
                    else
                    {
                        log.Error($"device key {ds.Key} is not present in device list");
                    }
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        //private async Task OnMusicServerInformation(MusicServerInformation m)
        //{
        //    serverDeadTimer.Reset();
        //    bool hasStateChanged = this.musicServerState.Connected == false;
        //    this.musicServerState.Connected = true;
        //    this.musicServerState.Url = m.Url;
        //    if (this.deviceUpdater == null)
        //    {
        //        StartFindLocalAudioDevicesTask();
        //    }
        //    if (hasStateChanged)
        //    {
        //        await this.SendPlayerInformation();
        //    }

        //    if (pausables.Any(x => x.IsPaused))
        //    {
        //        ResumeTasks();
        //    }
        //    await EnsureHubConnected();
        //}
        private async Task SendPlayerInformation()
        {
            //int retryCount = 10;
            if (this.msl.IsServerActive())
            {
                try
                {
                    using (var msc = new MusicServerClient(this.msl.GetServerUrl(), lf.CreateLogger<MusicServerClient>()))
                    {
                        await msc.SendPlayerInformation(this.audioDevices.Select(x => x.Value.audioDevice).ToArray());
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
        }
        private async Task StartFindLocalAudioDevicesTask()
        {
            this.deviceUpdater = new FindLocalAudioDevices(environment, messengerOptions, musicPlayerOptions, musicPlayerCancellationToken, lf, async (l) =>
            {
                return await ConfirmAudioDevicesWithMusicServer(l);
                //this.playerInformationTask.SetAudioDevices(this.audioDevices.Select(x => x.Value.audioDevice).ToArray());
            });
            await this.deviceUpdater.Start();
            //this.taskList.Add(Task.Run(() =>
            //{
            //    lock (sentinel)
            //    {
            //        pausables.Add(this.deviceUpdater);
            //        this.deviceUpdater.Start().ContinueWith(deviceUpdater.OnException, TaskContinuationOptions.OnlyOnFaulted);
            //    }

            //}, musicPlayerCancellationToken));
        }
        private async Task<bool> ConfirmAudioDevicesWithMusicServer(IEnumerable<AudioDevice> listOfLocalAudioDevices)
        {
            bool result = false;
            try
            {
                if (this.msl.IsServerActive())
                {
                    var currentDevices = this.audioDevices.Select(x => x.Value.audioDevice);
                    foreach (var kvp in audioDevices.ToArray())
                    {
                        if (listOfLocalAudioDevices.SingleOrDefault(x => string.Compare(x.Name, kvp.Value.audioDevice.Name, true) == 0) == null)
                        {
                            await RemoveDevice(kvp.Key);
                        }
                    }
                    using (var msc = new MusicServerClient(this.msl.GetServerUrl(), lf.CreateLogger<MusicServerClient>()))
                    {
                        foreach (var localDevice in listOfLocalAudioDevices)
                        {
                            var confirmedDevice = await msc.ConfirmDevice(localDevice);
                            //if (confirmedDevice != null)
                            //{
                            //    if (confirmedDevice.Enabled)
                            //    {
                            //        if (!audioDevices.ContainsKey(confirmedDevice.Key))
                            //        {
                            //            // add the device to music player dictionary
                            //            AddAudioDevice(confirmedDevice);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        // device is disabled at the music server
                            //        if (audioDevices.ContainsKey(confirmedDevice.Key))
                            //        {
                            //            await RemoveDevice(confirmedDevice.Key);
                            //        }
                            //    }
                            //}
                        }
                    }
                    result = true;
                }
                else
                {
                    log.Warning($"Cannot confirm devices as music server is not available");
                }

            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return result;
        }
        private void AddAudioDevice(AudioDevice d)
        {
            lock (sentinel)
            {
                if (!this.audioDevices.ContainsKey(d.Key))
                {
                    var dpt = StartDevicePlayer(d);
                    if (dpt != null)
                    {
                        if (!this.audioDevices.TryAdd(d.Key, (d, dpt)))
                        {
                            log.Error($"could not add device {d.DisplayName}, key {d.Key} already present - should not happen!!");
                        }
                        else
                        {
                            log.Information($"added device {d.DisplayName}, key {d.Key} ");
                        }
                    }
                }
                else
                {
                    log.Error($"could not add device {d.DisplayName}, key {d.Key} already present");
                    Debugger.Break();
                }
            }

        }
        private async Task RemoveDevice(string key)
        {
            DevicePlayerTask devicePlayerTask = null;
            AudioDevice audioDevice = null;
            lock (sentinel)
            {
                (audioDevice, devicePlayerTask) = audioDevices[key];
                devicePlayerTask.CancellationTokenSource.Cancel();

                audioDevices.TryRemove(key, out (AudioDevice t1, DevicePlayerTask t2) value);
                log.Information($"{audioDevice.DisplayName} removed from music player dictionary");
            }
            await Task.WhenAny(devicePlayerTask.Task);
        }
        //private async Task EnsureHubConnected()
        //{
        //    if (this.hubConnection == null)
        //    {
        //        this.hubConnection = new HubConnectionBuilder()
        //            .WithUrl($"{musicServerState.Url}/playhub")
        //            .Build();
        //        this.hubConnection.Closed += async (error) =>
        //        {
        //            if (error != null)
        //            {
        //                log.Debug(error.Message);
        //            }
        //            log.Warning($"hub connection closed");
        //            await Task.Delay(new Random().Next(0, 5) * 1000);
        //            await hubConnection.StartAsync();
        //            log.Information($"hub connection restarted");
        //        };
        //        this.hubConnection.On<AudioDevice>("SendDeviceNameChanged", (d) =>
        //        {
        //            //log.Information($"signalr: SendDeviceNameChanged {d.DisplayName}");
        //            if (this.audioDevices.ContainsKey(d.Key))
        //            {
        //                audioDevices[d.Key].audioDevice.DisplayName = d.DisplayName;
        //            }
        //        });
        //        this.hubConnection.On<AudioDevice>("SendDeviceEnabled", (d) =>
        //        {
        //            //log.Information($"signalr: SendDeviceEnabled {d.DisplayName}");
        //            if (!this.audioDevices.ContainsKey(d.Key))
        //            {
        //                if (d.Type != AudioDeviceType.Browser)
        //                {
        //                    AddAudioDevice(d);
        //                }
        //            }
        //        });
        //        this.hubConnection.On<AudioDevice>("SendDeviceDisabled", async (d) =>
        //        {
        //            //log.Information($"signalr: SendDeviceDisabled {d.DisplayName}");
        //            if (this.audioDevices.ContainsKey(d.Key))
        //            {
        //                await RemoveDevice(d.Key);
        //            }
        //        });
        //        await this.hubConnection.StartAsync();
        //        log.Information($"hub connection restarted");
        //    }
        //}
        //private void PauseTasks()
        //{
        //    lock (sentinel)
        //    {
        //        // log.Information($"@{stopwatch.Elapsed.TotalMilliseconds} milliseconds, music server not running/failed");
        //        //stopwatch.Restart();
        //        foreach (var pausable in pausables)
        //        {
        //            pausable.Pause();
        //        }
        //        foreach (var kvp in audioDevices)
        //        {
        //            kvp.Value.devicePlayerTask.DevicePlayer.OnEvent(PlayerEvents.Reset);
        //            log.Information($"PauseTasks(): device {kvp.Value.audioDevice.DisplayName} reset");
        //        }
        //    }
        //}
        //private void ResumeTasks()
        //{
        //    lock (sentinel)
        //    {
        //        log.Information($"music server reconnected");
        //        foreach (var pausable in pausables)
        //        {
        //            pausable.Resume();
        //        }
        //    }
        //}
        //private void onServerDeadTimer()
        //{
        //    musicServerState.Connected = false;
        //    PauseTasks();
        //    log.Information($"music server timedout");
        //}
    }
}
