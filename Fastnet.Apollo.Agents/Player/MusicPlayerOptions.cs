using Fastnet.Music.Core;
using System;

namespace Fastnet.Apollo.Agents
{
    public class MusicPlayerOptions
    {
        //public string LocalCIDR { get; set; }
        public int PlayerPort { get; set; }
        public PlayIntervals Intervals { get; set; }
        public AudioDeviceType[] DisabledAudioTypes { get; set; }
        [Obsolete]
        public bool UseDefaultDeviceOnly { get; set; }
        public bool TraceLMSApi { get; set; }
        public string LogitechServerUrl { get; set; }
        public int DeviceStatusUpdateInterval { get; set; } // milliseconds
        public int WaitingForEventTimer { get; set; }
        public int SqueezeboxClassicMaxRate { get; set; }
        public int SqueezeboxReceiverMaxRate { get; set; }
        public int SqueezeboxRadioMaxRate { get; set; }
        public int SqueezeboxTouchMaxRate { get; set; }
        public int MulticastReportInterval { get; set; }
        public MusicPlayerOptions()
        {
            Intervals = new PlayIntervals();
            DeviceStatusUpdateInterval = 3000;
            WaitingForEventTimer = DeviceStatusUpdateInterval * 3;// 6000;
            MulticastReportInterval = 6 * 5;// every five minutes
        }
    }
}
