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
        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
            {
                (var fullNamesDirectory, var compressedNamesDirectory) = GetArtistDirectories(artistSet);
                ProcessArtistSet(artistSet, GetWorks(artistSet), fullNamesDirectory, compressedNamesDirectory);
            }
            return Task.CompletedTask;
        }
        private void ProcessArtistSet(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo fullNamesDirectory, DirectoryInfo compressedNamesDirectory)
        {
            (var _, var invalidWorkFolders) = ValidateWorkFolders(artistSet, works, fullNamesDirectory);
            RemoveInvalidFolders(invalidWorkFolders);
            (var _, var invalidCompressedWorkFolders) = ValidateCompressedWorkFolders(artistSet, works, compressedNamesDirectory);
            RemoveInvalidFolders(invalidCompressedWorkFolders);
            //var works = artist.Works.OrderBy(w => w.Name);
            foreach (var work in works)
            {
                PortWorkUsingFullnames(fullNamesDirectory, work);
                PortWorkUsingCompressedNames(compressedNamesDirectory, work);
            }
        }
        //protected override void ProcessArtist(Artist artist, DirectoryInfo artistDirectory)
        //{
        //    //(var validFolders, var invalidCompressedWorkFolders) = ValidateCompressedWorkFolders(artist, artistDirectory);
        //    (var _, var invalidCompressedWorkFolders) = ValidateCompressedWorkFolders(artist, artistDirectory);
        //    (var _, var invalidWorkFolders) = ValidateWorkFolders(artist, artistDirectory);
        //    RemoveInvalidFolders(invalidCompressedWorkFolders.Union(invalidWorkFolders);
        //    var works = artist.Works.OrderBy(w => w.Name);
        //    foreach(var work in works)
        //    {
        //        CopyWorkUsingCompressedNames(artistDirectory, work);
        //    }
        //}
        private void PortWorkUsingFullnames(DirectoryInfo artistDirectory, Work work)
        {
            var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetPathSafeName(work.Name)));// GetNextDirectory(artistDirectory, "A");
            var (validfiles, invalidfiles) = ValidateTracksUsingFullNames(work, albumDirectory);
            foreach (var filename in invalidfiles)
            {
                var fi = new FileInfo(filename);
                if (fi.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    fi.Attributes &= ~fi.Attributes;
                    log.Information($"{fi.FullName} read only attribute unset");
                }
                fi.Delete();
                log.Information($"{fi.FullName} deleted");
            }
            var trackList = work.Tracks.ToArray();
            foreach (var track in work.Tracks)
            {
                //var filename = GetCompressedName(track, trackList); // GetNextMusicFilename(albumDirectory, "M");
                var filename = $"{track.Number:#00} {GetPathSafeName(track.Title)}";
                CopyTrack(track, albumDirectory, filename);
            }
        }
        private void PortWorkUsingCompressedNames(DirectoryInfo artistDirectory, Work work)
        {
            var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetCompressedName(work)));// GetNextDirectory(artistDirectory, "A");
            var (validfiles, invalidfiles) = ValidateTracksUsingCompressedNames(work, albumDirectory);
            foreach (var filename in invalidfiles)
            {
                var fi = new FileInfo(filename);
                if (fi.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    fi.Attributes &= ~fi.Attributes;
                    log.Information($"{fi.FullName} read only attribute unset");
                }
                fi.Delete();
                log.Information($"{fi.FullName} deleted");
            }
            var trackList = work.Tracks.ToArray();
            foreach (var track in work.Tracks)
            {
                var filename = GetCompressedName(track, trackList); // GetNextMusicFilename(albumDirectory, "M");
                CopyTrack(track, albumDirectory, filename);
            }
        }

        //protected override string GetCompressedName(Performance performance)
        //{
        //    throw new InvalidOperationException($"method not applicable to {musicStyle}");
        //}
        internal override CopiedTags LoadTags(Track track)
        {
            var ct = new CopiedTags
            {
                //Performers = new string[] { track.Work.Artist.Name },
                Performers = track.Work.Artists.Select(x => x.Name).ToArray(),
                Track = (uint)track.Number,
                Title = track.Title,
                //Pictures = new[] { new Picture(track.Work.Cover.Data) },
                Genres = new string[] { this.musicStyle.ToDescription() }
            };
            SetPicture(ct, track);
            if (!string.IsNullOrWhiteSpace(track.Work.DisambiguationName))
            {
                ct.Album = track.Work.DisambiguationName;
            }
            else
            {
                ct.Album = track.Work.Name;
            }
            if (track.Work.Type == OpusType.Singles)
            {
                ct.Album = $"{ct.Performers[0]} - Singles";
            }
            return ct;
        }
        private string GetCompressedName(Work work)
        {
            if (string.IsNullOrWhiteSpace(work.CompressedName))
            {
                //var existingNames = work.Artist.Works
                //    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                //    .Select(x => x.CompressedName);
                var artist = work.Artists.First();
                var existingNames = artist.Works
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                work.CompressedName = GetNextUniqueName("A", existingNames);
                musicDb.SaveChanges();
            }
            return work.CompressedName;
        }
        private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateCompressedWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory)
        {
            var existingFolders = artistDirectory.EnumerateDirectories();
            var compressedNames = works
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                var work = works.Single(x => x.CompressedName == di.Name);
                log.Trace($"Artist {artistSet.GetNames()}, valid folder {di.Name} ({work.Name})");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"Artist {artistSet.GetNames()}, invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }
        private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory)
        {
            var existingFolders = artistDirectory.EnumerateDirectories();
            var workNames = works
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .Select(x => GetPathSafeName(x.Name));
            var validFolders = existingFolders.Where(f => workNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                var work = works.Single(x => x.Name == di.Name);
                log.Trace($"Artist {artistSet.GetNames()}, valid folder {di.Name} ({work.Name})");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"Artist {artistSet.GetNames()}, invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }
        private (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracksUsingCompressedNames(Work work, DirectoryInfo workDirectory)
        {
            var existingFiles = workDirectory.EnumerateFiles();//.Select(fi => Path.GetFileNameWithoutExtension(fi.Name));
            var compressedNames = work.Tracks
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFiles = existingFiles.Where(f => compressedNames.Contains(Path.GetFileNameWithoutExtension(f.Name), StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, new FileInfoComparer());//, StringComparer.InvariantCultureIgnoreCase);
            foreach (var file in validFiles)
            {
                var track = work.Tracks.Single(x => x.CompressedName == Path.GetFileNameWithoutExtension(file.Name));
                log.Trace($"Work {work.Name}, valid file {file} ({track.Title})");
            }
            foreach (var file in invalidFiles)
            {
                log.Information($"Work {work.Name}, invalid file {file}");
            }
            return (validFiles.Select(x => x.FullName), invalidFiles.Select(x => x.FullName));
        }
        private (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracksUsingFullNames(Work work, DirectoryInfo workDirectory)
        {
            var existingFiles = workDirectory.EnumerateFiles();//.Select(fi => Path.GetFileNameWithoutExtension(fi.Name));
            var names = work.Tracks
                //.Where(w => !string.IsNullOrWhiteSpace(w.CompressedName)) //$"{track.Number:#00} {track.Title}"
                .Select(x => $"{x.Number:#00} {GetPathSafeName(x.Title)}");
            var validFiles = existingFiles.Where(f => names.Contains(Path.GetFileNameWithoutExtension(f.Name), StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, new FileInfoComparer());//, StringComparer.InvariantCultureIgnoreCase);
            //foreach (var file in validFiles)
            //{
            //    var track = work.Tracks.Single(x => x.Title == Path.GetFileNameWithoutExtension(file.Name));
            //    log.Trace($"Work {work.Name}, valid file {file} ({track.Title})");
            //}
            //foreach (var file in invalidFiles)
            //{
            //    log.Information($"Work {work.Name}, invalid file {file}");
            //}
            return (validFiles.Select(x => x.FullName), invalidFiles.Select(x => x.FullName));
        }
    }
}
