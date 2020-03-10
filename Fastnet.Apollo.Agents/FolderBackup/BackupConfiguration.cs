using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class BackupConfiguration
    {
        public BackupDefinition[] Definitions { get; set; }
    }
    public class BackupTask : SinglePipelineTask
    {
        private readonly BackupConfiguration backupConfiguration;
        public BackupTask(IOptions<BackupConfiguration> config, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            backupConfiguration = config.Value;
        }

        protected async override Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            log.Information("start");
            await Task.Delay(0);
            return null as ITaskState;
        }
    }
}
