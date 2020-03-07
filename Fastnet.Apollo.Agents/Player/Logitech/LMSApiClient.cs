using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public abstract class LMSApiClient : WebApiClient
    {
        protected readonly MusicPlayerOptions mpo;
        public LMSApiClient(MusicPlayerOptions mpo, ILogger logger) : base(mpo.LogitechServerUrl, logger)
        {
            this.mpo = mpo;
        }
        protected async Task<T> PostJsonAsync<T>(string json)
        {
            JObject jo = JObject.Parse(json);
            var r = await this.PostJsonAsync<JObject, T>(GetJsonRpc(), jo);
            if (mpo.TraceLMSApi)
            {
                log.Trace($"{json} send to {this.BaseAddress}, received {r.ToJson()}");
            }
            return r;
        }
        protected async Task PostJsonAsync(string json)
        {
            JObject jo = JObject.Parse(json);
            await this.PostAsync<JObject>(GetJsonRpc(), jo);
            if (mpo.TraceLMSApi)
            {
                log.Trace($"{json} send to {this.BaseAddress}");
            }
            return;
        }
        private string GetJsonRpc()
        {
            return $"jsonrpc.js";
        }
    }
}
