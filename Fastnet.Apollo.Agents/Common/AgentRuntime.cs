using Fastnet.Core.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public enum AgentType
    {
        Scheduled,
        Service
    }
    public abstract class AgentRuntime
    {
        private SchedulerService.ScheduledTaskWrapper taskWrapper;
        public AgentName Name { get; set; }
        public AgentType Type { get; set; } = AgentType.Scheduled;
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public bool IsRunning => GetIsRunning();
        public string ScheduleDescription => Type == AgentType.Scheduled ? taskWrapper?.GetScheduleDescription() ?? string.Empty : string.Empty;
        [DisplayFormat(DataFormatString = @"{0:ddMMMyyyy HH:mm}")]
        public DateTime NextRunTime => Type == AgentType.Scheduled ? taskWrapper?.GetNextRunTime() ?? DateTime.MinValue : DateTime.MinValue;
        public bool IsTaskSet => taskWrapper != null;

        public AgentRuntime()
        {

        }
        public AgentRuntime(Agent agent)
        {
            this.Name = agent.Name;
            this.Enabled = agent.Enabled;
            this.Description = agent.Description;
        }
        public void SetTask(SchedulerService.ScheduledTaskWrapper task)
        {
            this.taskWrapper = task;
        }
        private bool GetIsRunning()
        {
            if (Type == AgentType.Scheduled && taskWrapper != null)
            {
                return taskWrapper.IsRunning;
            }
            return false;
        }
    }
    public class PortableMusicAgent : AgentRuntime
    {
        public PortableMusicAgent()
        {

        }
        public PortableMusicAgent(Agent agent) : base(agent)
        {
        }

        public string PortableLibraryRoot { get; set; }
    }
    public class MusicPlayerAgent : AgentRuntime
    {
        public MusicPlayerAgent()
        {

        }
        public MusicPlayerAgent(Agent agent) : base(agent)
        {
        }
    }
    public class MusicLibraryCopierAgent : AgentRuntime
    {
        public MusicLibraryCopierAgent()
        {

        }
        public MusicLibraryCopierAgent(Agent agent) : base(agent)
        {
        }
    }
    
    public class FolderBackupAgent : AgentRuntime
    {
        public IEnumerable<BackupDefinition> Definitions { get; set; } = Enumerable.Empty<BackupDefinition>();
        public FolderBackupAgent()
        {

        }
        public FolderBackupAgent(Agent agent) : base(agent)
        {
        }
    }
    public class WebSite
    {
        private string offlineFile => Path.Combine(PhysicalPath, "app_offline.htm");
        public string Name { get; set; }
        public string PhysicalPath { get; set; }
        public int Port { get; set; }
        public IEnumerable<string> Zipfiles = Enumerable.Empty<string>();
        public async Task StopAsync()
        {
            if (!IsStopped())
            {
                await CreateOfflineFile();
            }
        }
        public void Start()
        {
            if(IsStopped())
            {
                File.Delete(offlineFile);
            }
        }
        public bool IsStopped()
        {
            return File.Exists(this.offlineFile);
        }
        private async Task CreateOfflineFile()
        {
            var lines = new string[]
            {
                @"<p></p>",
                @"<h3>This site is down for maintenance</h3>",
                @"<p>Please return later</p>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
                @"<div style='display:none'>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</div>",
            };
            await File.WriteAllLinesAsync(this.offlineFile, lines);
        }
    }
    public class WebDatabaseBackupAgent : AgentRuntime
    {
        public string BackupFolder { get; set; }
        public string WorkingFolder { get; set; }
        public List<WebSite> Sites = new List<WebSite>();
   
        public WebDatabaseBackupAgent()
        {

        }
        public WebDatabaseBackupAgent(Agent agent) : base(agent)
        {
        }
        public void AddSite(WebSite ws)
        {
            Sites.Add(ws);
        }
    }
}
