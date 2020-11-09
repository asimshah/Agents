using Fastnet.Core;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class MusicServerListener
    {
        public Func<bool, Task> StateChanged;
        public Func<AudioDevice, Task> DeviceEnabled;
        public Func<AudioDevice, Task> DeviceDisabled;
        public Func<AudioDevice, Task> DeviceNameChanged;
        private enum HubConnectingState
        {
            Disconnected,
            Connected,
            Connecting,
            Disconnecting,
        }
        private long counter = 0L;
        private bool serverActive = false;
        private HubConnectingState hcs = HubConnectingState.Disconnected;
        private DeadmanTimer serverDeadTimer;
        private string musicServerUrl;
        private HubConnection hubConnection;
        private readonly ILogger log;
        private readonly Messenger messenger;
        private readonly MusicPlayerOptions options;
        public MusicServerListener(Messenger messenger, IOptions<MusicPlayerOptions> mpo, ILogger<MusicServerListener> logger)
        {
            this.messenger = messenger;
            this.log = logger;
            this.options = mpo.Value;
            //this.msiInterval = mpo.Value.Intervals.ServerInformationBroadcastInterval;
            this.Start();
        }
        private void Start()
        {
            var interval = options.Intervals.ServerInformationBroadcastInterval;
            serverDeadTimer = new DeadmanTimer(interval * 3, interval * 2,
                async () => await onServerDeadTimerAsync());
            serverDeadTimer.Start();
            messenger.AddMulticastSubscription<MusicServerInformation>(async (m) => { await OnMusicServerInformation(m); });
            //Task.Run(async () =>
            //{
            //    while (this.serverActive == false)
            //    {
            //        await Task.Delay(2000);
            //        log.Information($"waiting for msi ...");
            //    }
            //    //await this.ConnectMusicServerHubAsync(500);
            //});
        }
        public bool IsServerActive()
        {
            return this.serverActive;
        }
        public string GetServerUrl()
        {
            return this.musicServerUrl;
        }
        private async Task OnMusicServerInformation(MusicServerInformation m)
        {
            try
            {
                serverDeadTimer.Reset();
                this.musicServerUrl = m.Url;
                bool hasChanged = this.serverActive == false;
                this.serverActive = true;
                if (hasChanged)
                {
                    await ConnectMusicServerHubAsync(500);
                    await RaiseStateChanged(true);
                }
                if ((counter % options.MulticastReportInterval) == 0)
                {
                    log.Trace($"recd MusicServerInformation [{counter.ToString()}]");
                }
                ++counter;
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task onServerDeadTimerAsync()
        {
            this.serverActive = false;
            //PauseTasks();
            log.Information($"music server information multicast time-out");
            await DisconnectMusicServerHub();
            await RaiseStateChanged(false);// this.StateChanged?.Invoke(false);
            //this.serverConnectionStateChanged(false);
        }
        private async Task DisconnectMusicServerHub()
        {
            if (this.hubConnection != null)
            {
                if (this.hcs != HubConnectingState.Disconnecting)
                {
                    this.hcs = HubConnectingState.Disconnecting;
                    await this.hubConnection.StopAsync();
                    await this.hubConnection.DisposeAsync();
                    this.hubConnection = null;
                    log.Information($"music server hub disconnected");
                }
            }
        }
        private async Task ConnectMusicServerHubAsync(int delayMilliSeconds)
        {
            if (this.serverActive)
            {
                if (this.hubConnection == null)
                {
                    this.hcs = HubConnectingState.Connecting;
                    log.Information($"music server hub connecting ...");
                    await Task.Delay(delayMilliSeconds);
                    //this.hubConnection = new HubConnectionBuilder()
                    //    .WithUrl($"{musicServerUrl}/playhub")
                    //    .Build();
                    this.hubConnection = new HubConnectionBuilder()
                        .WithUrl($"{musicServerUrl}/messagehub")
                        .Build();
                    log.Information($"music server url is {musicServerUrl}");
                    this.hubConnection.Closed += async (error) =>
                    {
                        if (error != null)
                        {
                            log.Debug(error.Message);
                        }
                        log.Warning($"hub connection closed");
                        this.hcs = HubConnectingState.Disconnected;
                        await this.ConnectMusicServerHubAsync(new Random().Next(0, 5) * 1000);
                    };
                    this.hubConnection.On<AudioDevice>("SendDeviceNameChanged", async (d) =>
                    {
                        try
                        {
                            if(string.Compare(d.HostMachine, Environment.MachineName, true) == 0)
                            {
                                await this.DeviceNameChanged?.Invoke(d);
                            }
                            //if (this.DeviceNameChanged != null)
                            //{
                            //    await this.DeviceNameChanged.Invoke(d);
                            //}
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe);
                        }
                    });
                    this.hubConnection.On<AudioDevice>("SendDeviceEnabled", async (d) =>
                    {
                        try
                        {
                            if (string.Compare(d.HostMachine, Environment.MachineName, true) == 0)
                            {
                                await this.DeviceEnabled?.Invoke(d);
                            }
                            //if (this.DeviceEnabled != null)
                            //{
                            //    await this.DeviceEnabled.Invoke(d);
                            //}
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe);
                        }
                    });
                    this.hubConnection.On<AudioDevice>("SendDeviceDisabled", async (d) =>
                    {
                        try
                        {
                            if (string.Compare(d.HostMachine, Environment.MachineName, true) == 0)
                            {
                                await this.DeviceDisabled?.Invoke(d);
                            }
                            //if (this.DeviceDisabled != null)
                            //{
                            //    await this.DeviceDisabled.Invoke(d);
                            //}
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe);
                        }
                    });
                    try
                    {
                        await this.hubConnection.StartAsync();
                        this.hcs = HubConnectingState.Connected;
                        log.Information($"hub connection restarted");
                    }
                    catch (Exception xe)
                    {
                        log.Warning($"error {xe.Message} occurred");
                        //Debugger.Break();
                        //throw;
                    }
                }
            }
            else
            {
                log.Warning($"Music Server is not active");
            }
        }
        private async Task RaiseStateChanged(bool tf)
        {
            if (this.StateChanged != null)
            {
                await this.StateChanged.Invoke(tf);
            }
        }
    }
}
