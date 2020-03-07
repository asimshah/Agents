using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class MusicServerClient : WebApiClient
    {
        public MusicServerClient(string url, ILogger<MusicServerClient> logger) : base(url, logger)
        {
        }
        public async Task<AudioDevice> ConfirmDevice(AudioDevice device)
        {
            return await this.PostAsync<AudioDevice, AudioDevice>("player/confirm/device", device);
        }
        public async Task SendPlayerInformation(AudioDevice[] devices)
        {
            await this.PostAsync("player/current/device/list", devices);
        }

        public async Task GetNextPlaylistItem(string deviceKey)
        {
            await this.GetAsync($"player/play/next/{deviceKey}");
        }
        //public async Task<StyleInformation[]> GetStyleInformation()
        //{
        //    return await this.GetDataResultObject<StyleInformation[]>($"lib/style/information");
        //}
        public async Task<MusicOptions> GetMusicOptions()
        {
            return await this.GetDataResultObject<MusicOptions>($"lib/music/options");
        }
    }
}
