using Fastnet.Core.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

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

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(lb => lb.AddRollingFile())
            .UseStartup<Startup>();
    }
}
