using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fastnet.Core.Logging;

namespace Fastnet.Apollo.Agents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var wh = CreateHostBuilder(args).Build();
            ApplicationLoggerFactory.Init(wh.Services);
            wh.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureLogging(lb => lb.AddRollingFile());
                    webBuilder.UseStartup<Startup>();
                });
    }
}
