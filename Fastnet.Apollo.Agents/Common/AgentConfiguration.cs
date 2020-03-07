using System.ComponentModel;

namespace Fastnet.Apollo.Agents
{
    public enum AgentName
    {
        //[Description("Music Resampler")]
        //MusicResampler,
        [Description("Portable Music Library")]
        PortableMusicLibrary,
        [Description("Music Player")]
        MusicPlayer,
        [Description("Music Library Copier")]
        MusicLibraryCopier,
        //[Description("Contact Synchroniser")]
        //ContactSynchroniser
    }
    public class Agent
    {
        public AgentName Name { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
    }
    public class AgentConfiguration
    {
        public Agent[] Agents { get; set; }
    }
}
