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
    public class PopularCompressedNames : BaseCompressedNameMethods
    {
        #region Public Constructors

        public PopularCompressedNames(PortabilityConfiguration portConfig) : base(MusicStyles.Popular, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        //public override sealed string GetTrackFileName(Track track, IEnumerable<Track> tracklist)
        //{
        //    throw new NotSupportedException();
        //}

        #endregion Public Methods
    }

    public class PopularFullNames : BaseFullNameMethods
    {
        #region Public Constructors

        public PopularFullNames(PortabilityConfiguration portConfig) : base(MusicStyles.Popular, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        //public override sealed string GetTrackFileName(Track track, IEnumerable<Track> tracklist)
        //{
        //    throw new NotSupportedException();
        //}

        #endregion Public Methods

    }
    public abstract class PortPopularMusic<T> : PortMusic<T> where T : BaseFolderMethods
    {
        #region Protected Constructors

        protected PortPopularMusic(T folderNamer, MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(folderNamer, musicOptions, MusicStyles.Popular, configuration, environment, portConfig)
        {
        }

        #endregion Protected Constructors

        #region Internal Methods

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

        #endregion Internal Methods

        #region Protected Methods

        protected abstract void PortWork(DirectoryInfo artistDirectory, Work work);
        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
            {
                var artistDirectory = folderNamer.GetArtistDirectory(artistSet);// GetArtistDirectories(artistSet);
                ProcessArtistSet(artistSet, GetWorks(artistSet), artistDirectory);
            }
            return Task.CompletedTask;
        }
        protected abstract (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracks(Work work, DirectoryInfo workDirectory);

        #endregion Protected Methods

        #region Private Methods

        private void ProcessArtistSet(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory/*, DirectoryInfo compressedNamesDirectory*/)
        {
            (var _, var invalidWorkFolders) = folderNamer.ValidateWorkFolders(artistSet, works, artistDirectory);
            folderNamer.RemoveInvalidFolders(invalidWorkFolders);
            //(var _, var invalidCompressedWorkFolders) = ValidateCompressedWorkFolders(artistSet, works, compressedNamesDirectory);
            //RemoveInvalidFolders(invalidCompressedWorkFolders);
            //var works = artist.Works.OrderBy(w => w.Name);
            foreach (var work in works)
            {
                PortWork(artistDirectory, work);
                //PortWorkUsingFullnames(fullNamesDirectory, work);
                //PortWorkUsingCompressedNames(compressedNamesDirectory, work);
            }
        }
        private IEnumerable<Work> GetWorks(ArtistSet artistSet)
        {
            var allWorksByTheseArtists = musicDb.ArtistWorkList.Where(x => artistSet.ArtistIds.Contains(x.ArtistId))
                .Select(x => x.Work).Distinct();
            var temp = allWorksByTheseArtists.AsEnumerable()
                .Join(musicDb.ArtistWorkList, w => w.Id, aw => aw.WorkId, (w, aw) => aw);
            var works = temp.GroupBy(x => x.Work)
                .Where(x => artistSet.Matches(x.Select(z => z.ArtistId)))
                .Select(x => x.Key);
            return works;
        }
        #endregion Private Methods
    }
    public class PortPopularMusicCN : PortPopularMusic<PopularCompressedNames>
    {
        #region Public Constructors

        public PortPopularMusicCN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(new PopularCompressedNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors

        #region Protected Methods

        protected override void PortWork(DirectoryInfo artistDirectory, Work work)
        {
            var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetCompressedName(work)));// GetNextDirectory(artistDirectory, "A");
            var (validfiles, invalidfiles) = ValidateTracks(work, albumDirectory);
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
        protected override (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracks(Work work, DirectoryInfo workDirectory)
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

        #endregion Protected Methods

        #region Private Methods

        //internal override CopiedTags LoadTags(Track track)
        //{
        //    throw new NotImplementedException();
        //}
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
        private string GetNextUniqueName(string initialLetter, IEnumerable<string> existingNames)
        {
            bool done = false;
            int i = 0;
            string name;// = null;
            do
            {
                ++i;
                name = $"{initialLetter}{i.ToString("x6")}";
                if (!existingNames.Contains(name))
                {
                    done = true;
                }
            } while (!done);
            return name;
        }
        private string GetCompressedName(Track track, IEnumerable<Track> trackList)
        {
            if (string.IsNullOrWhiteSpace(track.CompressedName))
            {
                var existingNames = trackList
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                track.CompressedName = GetNextUniqueName("M", existingNames);

                musicDb.SaveChanges();
            }
            return track.CompressedName;
        }

        #endregion Private Methods
    }
    public class PortPopularMusicFN : PortPopularMusic<PopularFullNames>
    {
        #region Public Constructors

        public PortPopularMusicFN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment,
            PortabilityConfiguration portConfig) : base(new PopularFullNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors

        #region Protected Methods

        //protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        //{
        //    foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
        //    {
        //        (var fullNamesDirectory, var compressedNamesDirectory) = GetArtistDirectories(artistSet);
        //        ProcessArtistSet(artistSet, GetWorks(artistSet), fullNamesDirectory, compressedNamesDirectory);
        //    }
        //    return Task.CompletedTask;
        //}
        protected override void PortWork(DirectoryInfo artistDirectory, Work work)
        {
            var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetPathSafeName(work.Name)));// GetNextDirectory(artistDirectory, "A");
            var (_, invalidfiles) = ValidateTracks(work, albumDirectory);
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
        //private void ProcessArtistSet(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo fullNamesDirectory, DirectoryInfo compressedNamesDirectory)
        //{
        //    (var _, var invalidWorkFolders) = ValidateWorkFolders(artistSet, works, fullNamesDirectory);
        //    RemoveInvalidFolders(invalidWorkFolders);
        //    (var _, var invalidCompressedWorkFolders) = ValidateCompressedWorkFolders(artistSet, works, compressedNamesDirectory);
        //    RemoveInvalidFolders(invalidCompressedWorkFolders);
        //    //var works = artist.Works.OrderBy(w => w.Name);
        //    foreach (var work in works)
        //    {
        //        PortWorkUsingFullnames(fullNamesDirectory, work);
        //        PortWorkUsingCompressedNames(compressedNamesDirectory, work);
        //    }
        //}
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
        //private void PortWorkUsingFullnames(DirectoryInfo artistDirectory, Work work)
        //{
        //    var albumDirectory = GetDirectoryInfo(Path.Combine(artistDirectory.FullName, GetPathSafeName(work.Name)));// GetNextDirectory(artistDirectory, "A");
        //    var (_, invalidfiles) = ValidateTracks(work, albumDirectory);
        //    foreach (var filename in invalidfiles)
        //    {
        //        var fi = new FileInfo(filename);
        //        if (fi.Attributes.HasFlag(FileAttributes.ReadOnly))
        //        {
        //            fi.Attributes &= ~fi.Attributes;
        //            log.Information($"{fi.FullName} read only attribute unset");
        //        }
        //        fi.Delete();
        //        log.Information($"{fi.FullName} deleted");
        //    }
        //    var trackList = work.Tracks.ToArray();
        //    foreach (var track in work.Tracks)
        //    {
        //        //var filename = GetCompressedName(track, trackList); // GetNextMusicFilename(albumDirectory, "M");
        //        var filename = $"{track.Number:#00} {GetPathSafeName(track.Title)}";
        //        CopyTrack(track, albumDirectory, filename);
        //    }
        //}


        //protected override string GetCompressedName(Performance performance)
        //{
        //    throw new InvalidOperationException($"method not applicable to {musicStyle}");
        //}
        //internal override CopiedTags LoadTags(Track track)
        //{
        //    var ct = new CopiedTags
        //    {
        //        //Performers = new string[] { track.Work.Artist.Name },
        //        Performers = track.Work.Artists.Select(x => x.Name).ToArray(),
        //        Track = (uint)track.Number,
        //        Title = track.Title,
        //        //Pictures = new[] { new Picture(track.Work.Cover.Data) },
        //        Genres = new string[] { this.musicStyle.ToDescription() }
        //    };
        //    SetPicture(ct, track);
        //    if (!string.IsNullOrWhiteSpace(track.Work.DisambiguationName))
        //    {
        //        ct.Album = track.Work.DisambiguationName;
        //    }
        //    else
        //    {
        //        ct.Album = track.Work.Name;
        //    }
        //    if (track.Work.Type == OpusType.Singles)
        //    {
        //        ct.Album = $"{ct.Performers[0]} - Singles";
        //    }
        //    return ct;
        //}

        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateCompressedWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory)
        //{
        //    var existingFolders = artistDirectory.EnumerateDirectories();
        //    var compressedNames = works
        //        .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var work = works.Single(x => x.CompressedName == di.Name);
        //        log.Trace($"Artist {artistSet.GetNames()}, valid folder {di.Name} ({work.Name})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Artist {artistSet.GetNames()}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory)
        //{
        //    var existingFolders = artistDirectory.EnumerateDirectories();
        //    var workNames = works
        //        .Where(w => !string.IsNullOrWhiteSpace(w.Name))
        //        .Select(x => GetPathSafeName(x.Name));
        //    var validFolders = existingFolders.Where(f => workNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var work = works.Single(x => x.Name == di.Name);
        //        log.Trace($"Artist {artistSet.GetNames()}, valid folder {di.Name} ({work.Name})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Artist {artistSet.GetNames()}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}

        protected override (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateTracks(Work work, DirectoryInfo workDirectory)
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

        #endregion Protected Methods
    }
}
