using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Apollo.Agents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fastnet.Apollo.Agents.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IOptionsMonitor<AgentConfiguration> configMonitor;
        private readonly ILogger<IndexModel> _logger;
        public string Machine => Environment.MachineName;
        [DisplayFormat(DataFormatString = @"{0:ddMMMyyyy \a\t HH:mm:ss}")]
        public DateTimeOffset Now => DateTimeOffset.Now;
        public IEnumerable<AgentRuntime> AgentRuntimes;
        private readonly AgentService agentService;
        public IndexModel(AgentService agentService, IOptionsMonitor<AgentConfiguration> acOptions, ILogger<IndexModel> logger)
        {
            this.agentService = agentService;
            _logger = logger;
        }

        public void OnGet()
        {
            this.AgentRuntimes = this.agentService.GetAgentRuntimes();
        }
    }
}
