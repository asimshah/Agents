namespace Fastnet.Apollo.Agents
{
    public class LogitechDevice //: AudioDevice
    {
        // these properties are only set as a result of server information call
        // they are not updated by the player information call
        // (this difference may be of value in the future!)
        public string MACAddress { get; set; }
        public string Name { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsConnected { get; set; }
        public bool IsPowerOn { get; set; }
        //public string UUID { get; set; }
        public string ModelName { get; set; }
    }
}
