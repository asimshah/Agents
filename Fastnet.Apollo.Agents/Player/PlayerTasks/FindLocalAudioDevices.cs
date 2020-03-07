using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Fastnet.Apollo.Agents
{
    public class FindLocalAudioDevices 
    {
        private readonly string localIPAddress;
        private Func<AudioDevice[], Task<bool>> updateList;
        private AudioDevice[] localDevices = new AudioDevice[0];
        private readonly MusicPlayerOptions mpo;
        private readonly MessengerOptions messengerOptions;
        private readonly ILogger log;
        private readonly ILoggerFactory lf;
        private readonly CancellationToken cancellationToken;
        private readonly IWebHostEnvironment environment;
        public FindLocalAudioDevices(IWebHostEnvironment webHostEnvironment, MessengerOptions messengerOptions, MusicPlayerOptions mpo, CancellationToken cancellationToken, ILoggerFactory loggerFactory,
            Func<AudioDevice[], Task<bool>> updateList)
        {
            this.environment = webHostEnvironment;
            this.messengerOptions = messengerOptions;
            this.mpo = mpo;
            this.lf = loggerFactory;
            this.log = lf.CreateLogger<FindLocalAudioDevices>();// log;
            this.cancellationToken = cancellationToken;
            this.updateList = updateList;
            if (environment.IsDevelopment())
            {
                this.localIPAddress = "localhost";
            }
            else
            {
                this.localIPAddress = GetLocalIPAddress().ToString();
            }
        }
        //public void Pause()
        //{

        //    log.Information($"paused");
        //}

        //public void Resume()
        //{

        //}
        public async Task Start()
        {
            log.Information($"started");

            while (true)
            {
                await UpdateOnChange();
                await Task.Delay(GetCurrentInterval());
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        public void OnException(Task task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception);
            }
            else
            {
                log.Warning($"did not expect to be here!!!!!");
            }
        }
        private async Task UpdateOnChange()
        {
            var list = await GetDeviceListAsync();
            var current = this.localDevices.Select(x => x.Name);
            var updated = list.Select(x => x.Name);
            var haschanged = current.Except(updated).Count() > 0 || updated.Except(current).Count() > 0;
            if (true || haschanged)
            {
                this.localDevices = list.ToArray();
                if (!await updateList.Invoke(this.localDevices))
                {
                    this.localDevices = Array.Empty<AudioDevice>();
                }
            }
        }
        private int GetCurrentInterval()
        {
            Debug.Assert(mpo.Intervals.LocalDeviceUpdateInterval > 0);
            return mpo.Intervals.LocalDeviceUpdateInterval;
        }
        private async Task<AudioDevice[]> GetDeviceListAsync()
        {
            var list = new List<AudioDevice>();
            AudioDevice AddAudioDevice(AudioDeviceType type, string name, string id, bool canReposition, bool isDefault)
            {
                if (list.SingleOrDefault(x => string.Compare(x.MACAddress, id, true) == 0) == null)
                {
                    var device = new AudioDevice
                    {
                        Type =  type,
                        Name = name,
                        IsDefault = isDefault,
                        CanReposition = canReposition,
                        HostMachine = Environment.MachineName.ToLower(),
                        Url = $"http://{localIPAddress}:{mpo.PlayerPort}",
                        MACAddress = id
                    };
                    list.Add(device);
                    return device;
                }
                return null;
            }

            foreach (var audioType in Enum.GetValues(typeof(AudioDeviceType)).Cast<AudioDeviceType>())
            {
                if(mpo.DisabledAudioTypes.Contains(audioType))
                {
                    continue;
                }
                switch (audioType)
                {
                    case AudioDeviceType.Unknown:
                        break;
                    //*** NB *** AsioOut not supported in naudio 1.9 for netstandard2.1 (as at 21Jan2020 - but jwosty is working on it: github #574, #538)
                    case AudioDeviceType.Asio:
                        // asio was originally designed for one device only
                        // here I take it to be the first device and mark that as the default one
                        foreach (var asio in AsioOut.GetDriverNames())
                        {
                            AddAudioDevice(AudioDeviceType.Asio, asio, asio, true, false);
                        }
                        break;
                    case AudioDeviceType.DirectSoundOut:
                        foreach (var dev in DirectSoundOut.Devices)
                        {
                            if("00000000-0000-0000-0000-000000000000" == dev.Guid.ToString())
                            {
                                continue;
                            }
                            bool msd = dev.Guid == DirectSoundOut.DSDEVID_DefaultPlayback;
                            AddAudioDevice(AudioDeviceType.DirectSoundOut, dev.Description, dev.Guid.ToString(), true, msd);
                        }
                        break;
                    case AudioDeviceType.Wasapi:
                        var enumerator = new MMDeviceEnumerator();
                        foreach (var de in enumerator.EnumerateAudioEndPoints(DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active))
                        {
                            var endPoint = new Guid(de.Properties[NAudio.CoreAudioApi.PropertyKeys.PKEY_AudioEndpoint_GUID].Value.ToString());
                            AddAudioDevice(AudioDeviceType.Wasapi, de.FriendlyName, id: endPoint.ToString(), canReposition: true, isDefault: false);
                        }
                        break;
                    case AudioDeviceType.Logitech:
                        try
                        {
                            int getMaxSampleRate(string model)
                            {
                                int sr = 0;
                                switch (model)
                                {
                                    case "Squeezebox Classic":
                                        sr = mpo.SqueezeboxClassicMaxRate;
                                        break;
                                    case "Squeezebox Receiver":
                                        sr = mpo.SqueezeboxReceiverMaxRate;
                                        break;
                                    case "Squeezebox Radio":
                                        sr = mpo.SqueezeboxRadioMaxRate;
                                        break;
                                    case "Squeezebox Touch":
                                        sr = mpo.SqueezeboxTouchMaxRate;
                                        break;
                                }
                                return sr;
                            }
                            // Note: lms repositions music files when you play something in its database
                            // but (apparently) does not do so if tell it play from a stream
                            // which is how LogitechDevicePlayer in the Apollo system works
                            var lmc = new LMSClient(mpo, this.lf.CreateLogger<LMSClient>());
                            var players = await lmc.ServerInformationAsync();
                            foreach(var x in players)
                            {
                                var device = AddAudioDevice(AudioDeviceType.Logitech, x.Name, x.MACAddress, false, false);
                                if (device != null)
                                {
                                    device.Capability = new AudioCapability { MaxSampleRate = getMaxSampleRate(x.ModelName) };
                                }
                            }
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe);
                        }
                        //Debugger.Break();
                        break;
                }
            }
            return list.ToArray();
        }
        private IPAddress GetLocalIPAddress()
        {
            var list = NetInfo.GetMatchingIPV4Addresses(messengerOptions.LocalCIDR);
            if (list.Count() > 1)
            {
                log.Warning($"Multiple local ipaddresses: {(string.Join(", ", list.Select(l => l.ToString()).ToArray()))}, cidr is {messengerOptions.LocalCIDR}, config error?");
            }
            return list.First();
        }
    }
}
