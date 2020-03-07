using Fastnet.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    class ProducerConsumerStream : Stream
    {
        private readonly MemoryStream innerStream;
        private long readPosition;
        private long writePosition;

        public ProducerConsumerStream()
        {
            innerStream = new MemoryStream();
        }

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public override void Flush()
        {
            lock (innerStream)
            {
                innerStream.Flush();
            }
        }

        public override long Length
        {
            get
            {
                lock (innerStream)
                {
                    return innerStream.Length;
                }
            }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (innerStream)
            {
                innerStream.Position = readPosition;
                int red = innerStream.Read(buffer, offset, count);
                readPosition = innerStream.Position;

                return red;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (innerStream)
            {
                innerStream.Position = writePosition;
                innerStream.Write(buffer, offset, count);
                writePosition = innerStream.Position;
            }
        }
    }

    public class MediaFoundationPlayer : IDisposable
    {
        private IWavePlayer player;
        private MMDevice mmDevice;
        private MediaFoundationReader reader;
        private Action onPlaybackStopped;
        private bool isRepositioning;
        private readonly ILogger log;
        public MediaFoundationPlayer(ILogger<MediaFoundationPlayer> logger)
        {
            this.log = logger;
        }
        public bool Initialise(AudioDevice device)
        {
            log.Information($"Initialising device {device.Name}");
            var enumerator = new MMDeviceEnumerator();
            mmDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active)
                .SingleOrDefault(x => string.Compare(x.FriendlyName, device.Name, true) == 0);
            if (mmDevice == null)
            {
                log.Error($"{device.Name} not found");
                return false;

            }
            else
            {
                log.Trace($"using device {mmDevice.FriendlyName}, {mmDevice.DeviceFriendlyName}");
            }
            return true;
        }
        public void PlayUrl(string url, float initialVolume, Action onPlaybackStopped)
        {
            try
            {
                this.onPlaybackStopped = onPlaybackStopped;
                int latency = 20;// move this to a config
                if (player != null)
                {
                    log.Warning($"Player is not null - play while playing");
                    StopPlayerInternal();
                }
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = initialVolume;// / 100.0f;
                player = new WasapiOut(mmDevice, AudioClientShareMode.Shared, true, latency);
                player.PlaybackStopped += Player_PlaybackStopped;
                reader = new MediaFoundationReader(url);
                player.Init(reader);
                player.Play();
            }
            catch (Exception xe)
            {
                log.Error(xe);
                throw;
            }

        }
        //private Stream ms;
        //public void PlayUrlWithStream(string url, float initialVolume, Action onPlaybackStopped)
        //{
        //    this.onPlaybackStopped = onPlaybackStopped;
        //    int latency = 20;// move this to a config
        //    if (player != null)
        //    {
        //        log.Warning($"Player is not null - play while playing");
        //        StopPlayerInternal();
        //    }
        //    mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = initialVolume;// / 100.0f;
        //    player = new WasapiOut(mmDevice, AudioClientShareMode.Shared, true, latency);
        //    player.PlaybackStopped += (s, e) =>
        //    {
        //        ms?.Dispose();
        //        Player_PlaybackStopped(s, e);
        //    };

        //    Task.Run(() =>
        //    {
        //        //reader = new StreamMediaFoundationReader(ms);
        //        reader = new MediaFoundationReader(url);
        //        player.Init(reader);
        //        player.Play();
        //        log.Information($"play url with stream finished");
        //    });
        //}
        public bool TogglePlayPause()
        {
            switch (player.PlaybackState)
            {
                case PlaybackState.Playing:
                    player?.Pause();
                    break;
                default:
                    player?.Play();
                    break;
            }
            return IsPlaying();
        }
        public void Reposition(float position)
        {
            if (player != null && reader != null && isRepositioning == false)
            {
                var requiredPosition = Convert.ToInt64(reader.Length * position);
                switch (player.PlaybackState)
                {
                    case PlaybackState.Paused:
                        break;
                    case PlaybackState.Playing:
                        isRepositioning = true;
                        player.Stop();
                        reader.Position = requiredPosition;
                        log.Debug($"repositioned to {reader.Position}");
                        player.Play();
                        isRepositioning = false;
                        break;
                    case PlaybackState.Stopped:
                        break;
                }
            }
            else
            {
                throw new Exception("No player or reader found");
            }
        }
        // reposition is a problem in that the music file is being downloaded from a url and
        // the required data may not yet have arrived if a position is requested further down the file.
        public void RepositionOld(float position)
        {
            if (player != null && reader != null)
            {
                bool canReposition = true;
                var requiredPosition = Convert.ToInt64(reader.Length * position);
                //requiredPosition = (requiredPosition % reader.BlockAlign) * reader.BlockAlign;
                if (requiredPosition > reader.Position)
                {
                    var offset = (int)(requiredPosition - reader.Position);
                    if (!reader.HasData(offset))
                    {
                        canReposition = false;
                    }
                }
                if (canReposition)
                {
                    reader.Position = requiredPosition;
                    log.Information($"repositioned to {reader.Position}");
                }
                else
                {
                    log.Warning($"repositioning to {requiredPosition} refused");
                }
                //return GetPlayerStatus();
            }
            else
            {
                throw new Exception("No player or reader found");
            }
        }
        public void SetVolume(float level)
        {
            if (player != null)
            {
                player.Volume = level;
            }
            else
            {
                throw new Exception("No player found");
            }
        }
        public bool IsPlaying()
        {
            return player.PlaybackState == PlaybackState.Playing;
        }
        public void Dispose()
        {
            this.player?.Dispose();
            this.mmDevice?.Dispose();
            this.reader?.Dispose();
        }
        public (PlaybackState state, float volume, TimeSpan currentTime, TimeSpan totalTime) GetStatusInformation()
        {
            return (this.player?.PlaybackState ?? PlaybackState.Stopped, this.player?.Volume ?? 0.0f,
                reader?.CurrentTime ?? TimeSpan.Zero, reader?.TotalTime ?? TimeSpan.Zero);
        }
        public void Stop()
        {
            StopPlayerInternal();
        }
        private void StopPlayerInternal()
        {
            if (player != null)
            {
                player.PlaybackStopped -= Player_PlaybackStopped;
                player?.Stop();
                player.Dispose();
                player = null;
            }
        }
        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // this should only happen when currently playing item finishes
            if (e.Exception != null)
            {
                log.Error(e.Exception);
            }
            if (!isRepositioning)
            {
                onPlaybackStopped();
            }
        }

    }
}
