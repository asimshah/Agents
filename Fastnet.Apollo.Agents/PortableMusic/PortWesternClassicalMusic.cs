using System.Linq;
using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Fastnet.Music.TagLib;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class PortWesternClassicalMusic : PortMusic
    {
        public PortWesternClassicalMusic(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment,
            PortabilityConfiguration portConfig) : base(musicOptions, MusicStyles.WesternClassical, configuration, environment, portConfig)
        {
        }
        protected override void ProcessArtist(Artist artist, DirectoryInfo artistDirectory)
        {
            (var validFolders, var invalidFolders) = ValidateCompositions(artist, artistDirectory);
            RemoveInvalidFolders(invalidFolders);
            var compositions = artist.Compositions.OrderBy(c => c.Name);
            foreach (var composition in compositions)
            {
                var compositionDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetCompressedName(composition)));// GetNextDirectory(artistDirectory, "C");// GetCompositionDirectory(artistDirectory, composition);
                ValidatePerformances(composition, compositionDirectory);
                foreach (var performance in composition.Performances)
                {
                    var performanceDirectory = GetDirectoryInfo(Path.Combine(compositionDirectory.FullName, GetCompressedName(performance))); //GetNextDirectory(compositionDirectory, "P");
                    ValidateMovements(performance, performanceDirectory);
                    var trackList = performance.Movements.ToArray();
                    foreach (var movement in performance.Movements)
                    {
                        var filename = GetCompressedName(movement, trackList);//  GetNextMusicFilename(performanceDirectory, "M");
                        CopyTrack(movement, performanceDirectory, filename);

                    }
                }
            }
        }
        internal override CopiedTags LoadTags(Track track)
        {
            var ct = new CopiedTags
            {
                Performers = new string[] { track.Performance.Composition.Artist.Name },
                Album = track.Performance.Composition.Name,
                Track = (uint)track.Number,
                Title = track.Title,
                Pictures = new[] { new Picture(track.Work.Cover.Data) },
                Genres = new string[] { this.musicStyle.ToDescription() }
            };
            var perfomanceCount = track.Performance.Composition.Performances.Count();
            if(perfomanceCount > 1)
            {
                var index = track.Performance.Composition.Performances.OrderBy(x => x.Performers).ToList().IndexOf(track.Performance);
                ct.Album = $"{track.Performance.Composition.Name} ({index + 1})";
            }
            return ct;
        }
        private string GetCompressedName(Composition composition)
        {
            if (string.IsNullOrWhiteSpace(composition.CompressedName))
            {
                var existingNames = composition.Artist.Compositions
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                composition.CompressedName = GetNextUniqueName("C", existingNames);
                musicDb.SaveChanges();
            }
            return composition.CompressedName;
        }
        private string GetCompressedName(Performance performance)
        {
            if (string.IsNullOrWhiteSpace(performance.CompressedName))
            {
                var existingNames = performance.Composition.Performances
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                performance.CompressedName = GetNextUniqueName("P", existingNames);
                musicDb.SaveChanges();
            }
            return performance.CompressedName;
        }
        private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateCompositions(Artist artist, DirectoryInfo artistDirectory)
        {
            var existingFolders = artistDirectory.EnumerateDirectories();
            var compressedNames = artist.Compositions
                .Where(c => !string.IsNullOrWhiteSpace(c.CompressedName))
                .Select(x => x.CompressedName);
            var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                //var composition = artist.Compositions.Single(x => x.CompressedName == di.Name);
                //log.Information($"Artist {artist.Name}, valid folder {di.Name} ({composition.Name})");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"Artist {artist.Name}, invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }
        private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidatePerformances(Composition composition, DirectoryInfo compositionDirectory)
        {
            var existingFolders = compositionDirectory.EnumerateDirectories();
            var compressedNames = composition.Performances // artist.Compositions
                .Where(c => !string.IsNullOrWhiteSpace(c.CompressedName))
                .Select(x => x.CompressedName);
            var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                //var performance = composition.Performances.Single(x => x.CompressedName == di.Name);
                //log.Information($"Composition {composition.Name}, valid folder {di.Name} ({performance.Performers})");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"Composer {composition.Artist.Name}, Composition {composition.Name} ({composition.CompressedName}), folder {di.Name}, performance not found in database");
            }
            return (validFolders, invalidFolders);
        }
        private (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateMovements(Performance performance, DirectoryInfo performanceDirectory)
        {
            var existingFiles = performanceDirectory.EnumerateFiles().Select(fi => Path.GetFileNameWithoutExtension(fi.Name));
            var compressedNames = performance.Movements // work.Tracks
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFiles = existingFiles.Where(f => compressedNames.Contains(f, StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, StringComparer.InvariantCultureIgnoreCase);
            foreach (var file in validFiles)
            {
                var movement = performance.Movements.Single(x => x.CompressedName == file);
                //log.Information($"Work {work.Name}, valid file {file} ({track.Title})");
            }
            foreach (var file in invalidFiles)
            {
                log.Information($"Performance {performance.Composition.Name}, {performance.Performers}, invalid file {file}");
            }
            return (validFiles, invalidFiles);
        }
    }
}




