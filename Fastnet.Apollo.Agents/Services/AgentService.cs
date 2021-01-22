using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Apollo.Agents.Services
{
    public class AgentService
    {
        private readonly IServiceProvider service;
        private readonly ILogger log;
        private readonly IOptionsMonitor<AgentConfiguration> configOptions;
        private readonly IOptionsMonitor<BackupConfiguration> backupConfig;
        private readonly IOptionsMonitor<PortabilityConfiguration> portabilityConfig;
        private readonly IOptionsMonitor<WebDbBackupConfiguration> webDbBackupConfig;
        private readonly SchedulerService schedulerService;
        private readonly IServiceProvider serviceProvider;
        public AgentService(
            IServiceProvider serviceProvider,
            SchedulerService schedulerService,
            IOptionsMonitor<AgentConfiguration> configOptions,
            IOptionsMonitor<BackupConfiguration> backupConfig,
            IOptionsMonitor<PortabilityConfiguration> portabilityConfig,
            IOptionsMonitor<WebDbBackupConfiguration> webDbBackupConfiguration,
            ILogger<AgentService> logger)
        {
            this.serviceProvider = serviceProvider;
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
                                if (IsAssociatedScheduledTaskOrServiceAvailable(fb))
                                {
                                    fb.Definitions = backupConfig.CurrentValue.Definitions.ToArray();
                                    list.Add(fb);
                                }
                            }
                            break;
                        case AgentName.MusicPlayer:
                            var mp = Create<MusicPlayerAgent>(item);// new MusicPlayerAgent(item);
                            if (IsAssociatedScheduledTaskOrServiceAvailable(mp))
                            {
                                list.Add(mp);
                            }
                            break;
                        case AgentName.PortableMusicLibrary:
                            var pma = Create<PortableMusicAgent>(item);
                            if (IsAssociatedScheduledTaskOrServiceAvailable(pma))
                            {
                                pma.PortableLibraryRoot = portabilityConfig.CurrentValue.PortableLibraryRoot;
                                list.Add(pma);
                            }
                            break;
                        case AgentName.WebDatabaseBackup:
                            var wdba = Create<WebDatabaseBackupAgent>(item);
                            if (IsAssociatedScheduledTaskOrServiceAvailable(wdba))
                            {
                                wdba.BackupFolder = webDbBackupConfig.CurrentValue.BackupFolder;
                                wdba.WorkingFolder = webDbBackupConfig.CurrentValue.WorkingFolder;
                                ScanLocalIIS(wdba);
                                list.Add(wdba);
                            }
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
                log.Trace($"site {site.Name} found {site.Bindings.Count()} bindings");
                foreach (var binding in site.Bindings)
                {
                    log.Trace($"binding {binding?.Host} port {binding?.EndPoint?.Port}");
                    var port = binding.EndPoint.Port;
                    //if (port != 80) // fastnet web sites never use port 80
                    //{
                        var physicalPath = app.VirtualDirectories.First().PhysicalPath;
                        var databasePath = Path.Combine(physicalPath, "Data");
                        var old_databasePath = Path.Combine(physicalPath, "App_Data");
                        if (Directory.Exists(databasePath) || Directory.Exists(old_databasePath))
                        {
                            wdba.AddSite(new WebSite { Name = site.Name, PhysicalPath = physicalPath, Port = port });
                        }
                        else
                        {
                            log.Trace($"Site {site.Name} physical path {physicalPath} does not have a data folder");
                        }
                    //}
                    //Debug.WriteLine($"Site {site.Name} port {binding.EndPoint.Port}");
                }
            }
        }
        private bool IsAssociatedScheduledTaskOrServiceAvailable(AgentRuntime ar)
        {
            var result = false;
            switch (ar.Type)
            {
                case AgentType.Scheduled:
                    result = ar.IsTaskSet;
                    break;
                case AgentType.Service:
                    switch(ar.Name)
                    {
                        case AgentName.MusicPlayer:
                            var player = this.serviceProvider.GetService<MusicPlayer>();
                            result = player != null;
                            break;
                    }
                    break;
            }
            return result;
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
