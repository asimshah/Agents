namespace Fastnet.Apollo.Agents
{
    public class LogitechDeviceStatus : LogitechDevice
    {
        public double Duration { get; set; }
        public double Position { get; set; }
        public float Volume { get; set; }
        public string Mode { get; set; }
        public string Title { get; set; }
        public string File { get; set; }
    }
}
