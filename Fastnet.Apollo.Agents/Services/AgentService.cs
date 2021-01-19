using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Web.Administration;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;

namespace Fastnet.Apollo.Agents.Services
{
    public class AgentService
    {
        private readonly ILogger log;
        private readonly IOptionsMonitor<AgentConfiguration> configOptions;
        private readonly IOptionsMonitor<BackupConfiguration> backupConfig;
        private readonly IOptionsMonitor<PortabilityConfiguration> portabilityConfig;
        private readonly IOptionsMonitor<WebDbBackupConfiguration> webDbBackupConfig;
        private readonly SchedulerService schedulerService;
        public AgentService(
            SchedulerService schedulerService,
            IOptionsMonitor<AgentConfiguration> configOptions,
            IOptionsMonitor<BackupConfiguration> backupConfig,
            IOptionsMonitor<PortabilityConfiguration> portabilityConfig,
            IOptionsMonitor<WebDbBackupConfiguration> webDbBackupConfiguration,
            ILogger<AgentService> logger)
        {
            this.schedulerService = schedulerService;
            this.configOptions = configOptions;
            this.backupConfig = backupConfig;
            this.portabilityConfig = portabilityConfig;
            this.webDbBackupConfig = webDbBackupConfiguration;
            this.log = logger;
        }
        public IEnumerable<AgentRuntime> GetAgentRuntimes()
        {
            var list = new List<AgentRuntime>();
            foreach (var item in this.configOptions.CurrentValue.Agents)
            {
                if (item.Enabled)
                {
                    switch (item.Name)
                    {
                        case AgentName.FolderBackup:
                            if (backupConfig.CurrentValue.Definitions.Length > 0)
                            {                               
                                var fb = Create<FolderBackupAgent>(item);
                                fb.Definitions = backupConfig.CurrentValue.Definitions.ToArray();
                                list.Add(fb);
                            }
                            break;
                        case AgentName.MusicPlayer:
                            var mp = Create<MusicPlayerAgent>(item);// new MusicPlayerAgent(item);
                            list.Add(mp);
                            break;
                        case AgentName.PortableMusicLibrary:
                            var pma = Create<PortableMusicAgent>(item);// new PortableMusicAgent(item);
                            pma.PortableLibraryRoot = portabilityConfig.CurrentValue.PortableLibraryRoot;
                            list.Add(pma);
                            break;
                        case AgentName.WebDatabaseBackup:
                            var wdba = Create<WebDatabaseBackupAgent>(item);
                            wdba.BackupFolder = webDbBackupConfig.CurrentValue.BackupFolder;
                            wdba.WorkingFolder = webDbBackupConfig.CurrentValue.WorkingFolder;
                            ScanLocalIIS(wdba);
                            list.Add(wdba);
                            break;
                        default:
                            break;
                    }
                }
            }
            return list;
        }
        public ServerManager GetIISServerManager()
        {
            var path = Path.Combine(Environment.SystemDirectory, "inetsrv", "config", "applicationHost.config");
            //var sm = new Microsoft.Web.Administration.ServerManager(@"C:\Windows\System32\inetsrv\config\applicationHost.config");
            return new ServerManager(path);
        }
        public void ScanLocalIIS(WebDatabaseBackupAgent wdba)
        {
            var sm = GetIISServerManager();
            foreach (var site in sm.Sites)
            {
                //Debug.WriteLine($"site {site.Name}");
                //site.Applications[0].VirtualDirectories[0].PhysicalPath
                var app = site.Applications.First();
                //log.Information($"site {site.Name} found {site.Bindings.Count()} bindings");
                foreach (var binding in site.Bindings)
                {
                    //log.Information($"binding {binding.Host} port {binding.EndPoint.Port}");
                    var port = binding.EndPoint.Port;
                    if (port != 80) // fastnet web sites never use port 80
                    {
                        var physicalPath = app.VirtualDirectories.First().PhysicalPath;
                        var databasePath = Path.Combine(physicalPath, "Data");
                        if (Directory.Exists(databasePath))
                        {
                            wdba.AddSite(new WebSite { Name = site.Name, PhysicalPath = physicalPath, Port = port });
                        }
                        else
                        {
                            log.Information($"Site {site.Name} physical path {physicalPath} does not have a data folder");
                        }
                    }
                    //Debug.WriteLine($"Site {site.Name} port {binding.EndPoint.Port}");
                }
            }
        }
        private T Create<T>(Agent agent) where T : AgentRuntime, new()
        {
            var ar = Activator.CreateInstance(typeof(T), agent) as T;
            switch (ar)
            {
                case WebDatabaseBackupAgent _:
                    ar.SetTask(this.schedulerService.GetTask<WebDbBackupTask>());
                    break;
                case FolderBackupAgent _:
                    ar.SetTask(this.schedulerService.GetTask<BackupTask>());
                    break;
                case PortableMusicAgent _:
                    ar.SetTask(this.schedulerService.GetTask<MusicPortingTask>());
                    break;
                case MusicPlayer _:
                    ar.Type = AgentType.Service;
                    //ar.SetTask(this.schedulerService.GetTask<Mu>());
                    break;
            }
            return ar;
        }
    }
}
