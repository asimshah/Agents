using Fastnet.Apollo.Agents.Services;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.Web.Administration;
using System.IO;
using System.IO.Compression;

namespace Fastnet.Apollo.Agents
{
    public class WebDbBackupTask : SinglePipelineTask
    {
        private readonly AgentService agentService;
        public WebDbBackupTask(AgentService agentService, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.agentService = agentService;
        }
        protected async override Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            log.Information("started");
            //await Task.Delay(0);
            var wdba = agentService.GetAgentRuntimes().OfType<WebDatabaseBackupAgent>().Single();
            foreach(var site in wdba.Sites)
            {
                await site.StopAsync();
                log.Information($"Site {site.Name} stopped");
                await Task.Delay(3000);
                CopyDatabase(site, wdba);
                site.Start();
            }
            return null as ITaskState;
        }
        public void CopyDatabase(WebSite site, WebDatabaseBackupAgent agent)
        {
            void copyFile(string file, string dest)
            {
                var fiFrom = new FileInfo(file);
                var to = Path.Combine(dest, fiFrom.Name);
                fiFrom.CopyTo(to);
                log.Information($"{file} copied to {to}");
            }
            var source = Path.Combine(site.PhysicalPath, "Data");
            var dest = Path.Combine(agent.WorkingFolder, site.Name);
            var wf = new DirectoryInfo(agent.WorkingFolder);
            if(!wf.Exists)
            {
                Directory.CreateDirectory(wf.FullName);
                log.Information($"{wf.FullName} created");
            }
            else
            {
                wf.Clear();
                log.Information($"{wf.FullName} cleared");
            }
            Directory.CreateDirectory(dest);
            log.Information($"{dest} created");
            var mdfFiles = Directory.EnumerateFiles(source, "*.mdf");
            foreach (var file in mdfFiles)
            {
                copyFile(file, dest);
            }
            var ldfFiles = Directory.EnumerateFiles(source, "*.ldf");
            foreach (var file in ldfFiles)
            {
                copyFile(file, dest);
            }
            CompressAndWrite(site, agent, dest);
            wf.Clear();
        }
        public void CompressAndWrite(WebSite site, WebDatabaseBackupAgent agent, string sourceDirectory)
        {
            if(!Directory.Exists(agent.BackupFolder))
            {
                Directory.CreateDirectory(agent.BackupFolder);
                log.Information($"{agent.BackupFolder} created");
            }
            var targetFolder = Path.Combine(agent.BackupFolder, site.Name);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                log.Information($"{targetFolder} created");
            }
            var now = DateTime.Now;
            var zipFile = Path.Combine(targetFolder, $"{site.Name}-{now.ToString("yyyyMMdd-HHmmss")}.zip");
            ZipFile.CreateFromDirectory(sourceDirectory, zipFile);
            log.Information($"{zipFile} created");
        }
    }
    public class WebDbBackupConfiguration
    {
        public string BackupFolder { get; set; }
        public string WorkingFolder { get; set; }
    }
}
