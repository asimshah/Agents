using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Apollo.Agents.Services;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fastnet.Apollo.Agents.Pages
{
    public class WebDbBackupModel : PageModel
    {
        public WebDatabaseBackupAgent Agent { get; set; }// => this.agentService.GetAgentRuntimes().Single(x => x.Name == AgentName.WebDatabaseBackup) as WebDatabaseBackupAgent;

        private readonly AgentService agentService;
        private readonly SchedulerService schedulerService;
        public WebDbBackupModel(SchedulerService schedulerService, AgentService agentService)
        {
            this.schedulerService = schedulerService;
            this.agentService = agentService;
            Initialise();
        }
        public async Task OnPostAsync()
        {
            await schedulerService.ExecuteNow<WebDbBackupTask>();

        }
        public void OnGet()
        {

        }

        private void Initialise()
        {
            Agent = this.agentService.GetAgentRuntimes().Single(x => x.Name == AgentName.WebDatabaseBackup) as WebDatabaseBackupAgent;


            foreach (var site in Agent.Sites)
            {
                var bf = Path.Combine(Agent.BackupFolder, site.Name);
                if (Directory.Exists(bf) && Directory.EnumerateFiles(bf, "*.zip").Count() > 0)
                {
                    var zipFiles = Directory.EnumerateFiles(bf, "*.zip");
                    site.Zipfiles = zipFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime);
                }
            }
        }
    }
}
