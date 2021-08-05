using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server
{
    public class BackupOptions
    {
        public int BackupHour { get; set; } = 3; // the hour (24hr UTC format) when backups shoudl start every day
        public int WaitForWebsiteOffline { get; set; } = 5000; // milliseconds to wait after creating app_offline.htm
    }
}
