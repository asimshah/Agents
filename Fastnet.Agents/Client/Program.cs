using Fastnet.Agents.Client.Services;
using Fastnet.Blazor.Controls;
using Fastnet.Blazor.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSimpleAuthentication<AuthenticationService>();
            builder.Services.AddScoped<BackupService>();
            builder.Services.AddFastnetBlazorControls();
            builder.Services.AddScoped<CoreService>();
            await builder.Build().RunAsync();
        }
    }
}
