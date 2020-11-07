using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Fastnet.Music.TagLib;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Fastnet.Apollo.Agents
{
    public class PortPopularMusic : PortMusic
    {
        public PortPopularMusic(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment,
            PortabilityConfiguration portConfig) : base(musicOptions, MusicStyles.Popular, configuration, environment, portConfig)
        {
        }
        protected override void ProcessArtist(Artist artist, DirectoryInfo artistDirectory)
        {
            log.Debug($"processing {artist.Name}");
            (var validFolders, var invalidFolders) = ValidateWorks(artist, artistDirectory);
            RemoveInvalidFolders(invalidFolders);
            var works = artist.Works.OrderBy(w => w.Name);
            foreach(var work in works)
            {
                var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetCompressedName(work)));// GetNextDirectory(artistDirectory, "A");
                ValidateTracks(work, albumDirectory);
                var trackList = work.Tracks.ToArray();
                foreach (var track in work.Tracks)
                {
                    var filename = GetCompressedName(track, trackList); // GetNextMusicFilename(albumDirectory, "M");
                    CopyTrack(track, albumDirectory, filename);
                }
            }
        }

        internal override CopiedTags LoadTags(Track track)
        {
            var ct = new CopiedTags
            {
                //Performers = new string[] { track.Work.Artist.Name },
                Performers = track.Work.Artists.Select(x => x.Name).ToArray(),
                Track = (uint)track.Number,
                Title = track.Title,
                Pictures = new[] { new Picture(track.Work.Cover.Data) },
                Genres = new string[] { this.musicStyle.ToDescription() }
            };
            if (!string.IsNullOrWhiteSpace(track.Work.DisambiguationName))
            {
                ct.Album = track.Work.DisambiguationName;
            }
            else
            {
                ct.Album = track.Work.Name;
            }
            if(track.Work.Type == OpusType.Singles)
            {
                ct.Album = $"{ct.Performers[0]} - Singles";
            }
            return ct;
        }
        private string GetCompressedName(Work work)
        {
            if (string.IsNullOrWhiteSpace(work.CompressedName))
            {
                var works = work.Artists.SelectMany(x => x.Works).Distinct();
                var existingNames = works // work.Artist.Works
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                work.CompressedName = GetNextUniqueName("A", existingNames);
                musicDb.SaveChanges();
            }
            return work.CompressedName;
        }
        private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateWorks(Artist artist, DirectoryInfo artistDirectory)
        {
            var existingFolders = artistDirectory.EnumerateDirectories();
            var compressedNames = artist.Works
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                //var work = artist.Works.Single(x => x.CompressedName == di.Name);
                //log.Information($"Artist {artist.Name}, valid folder {di.Name} ({work.Name})");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"Artist {artist.Name}, invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }
        private (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracks(Work work, DirectoryInfo workDirectory)
        {
            var existingFiles = workDirectory.EnumerateFiles().Select(fi => Path.GetFileNameWithoutExtension(fi.Name));
            var compressedNames = work.Tracks
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFiles = existingFiles.Where(f => compressedNames.Contains(f, StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, StringComparer.InvariantCultureIgnoreCase);
            foreach (var file in validFiles)
            {
                //var track = work.Tracks.Single(x => x.CompressedName == file);
                //log.Information($"Work {work.Name}, valid file {file} ({track.Title})");
            }
            foreach (var file in invalidFiles)
            {
                log.Information($"Work {work.Name}, invalid file {file}");
            }
            return (validFiles, invalidFiles);
        }

    }
}
