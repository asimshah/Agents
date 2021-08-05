using System;
using System.ComponentModel;

namespace Fastnet.Apollo.Agents
{
    public enum AgentName
    {
        [Description("Portable Music Library")]
        PortableMusicLibrary,
        [Description("Music Player")]
        MusicPlayer,
        [Description("Music Library Copier")]
        [Obsolete]
        MusicLibraryCopier,
        [Description("Folder Backup")]
        FolderBackup,
        [Description("Website DB backup")]
        WebDatabaseBackup
        //[Description("Contact Synchroniser")]
        //ContactSynchroniser
    }
}
