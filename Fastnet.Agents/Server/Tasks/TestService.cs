using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server.Tasks
{
    public class TestService : SinglePipelineTask
    {
        public TestService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            await Task.Yield();
            log.Information($"{DateTimeOffset.Now.ToDefaultWithTime()} executed");
            return null;
        }
    }
}
