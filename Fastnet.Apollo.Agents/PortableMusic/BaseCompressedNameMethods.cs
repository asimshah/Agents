using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fastnet.Apollo.Agents
{
    public abstract class BaseCompressedNameMethods : BaseFolderMethods
    {
        #region Private Fields

        private readonly Regex safeChars = new Regex(@"[^\sa-zA-Z0-9\p{L}]", RegexOptions.IgnoreCase);

        #endregion Private Fields

        #region Public Constructors

        public BaseCompressedNameMethods(MusicStyles style, PortabilityConfiguration portConfig) : base(style, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        public override DirectoryInfo GetArtistDirectory(ArtistSet artistSet)
        {
            var setName = GetCompressedName(artistSet);
            var setPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), setName);
            return GetDirectoryInfo(setPath);
            //setName = artistSet.GetNames();
            //setPath = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription(), setName);
            //var fullNames = GetDirectoryInfo(setPath);
            //return (fullNames, compressedNames);
        }
        public override DirectoryInfo GetArtistDirectory(Artist artist)
        {
            var compressedPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), artist.CompressedName);
            return GetDirectoryInfo(compressedPath);
        }
        //public override string GetPerformanceName(PerformanceTuple tuple)
        //{
        //    return GetCompressedName(tuple.Performance, tuple.Performance.Composition.Performances);
        //}
        public override string GetPerformanceName(PerformanceTuple tuple, IEnumerable<Performance> allPerformances = null)
        {
            return GetCompressedName(tuple.Performance, allPerformances ?? tuple.Performance.Composition.Performances);
        }

        #endregion Public Constructors

        #region Public Methods

        public override string GetTrackFileName(Track track, IEnumerable<Track> tracklist)
        {
            return GetCompressedName(track, tracklist);
        }
        public override void RemoveInvalidArtistDirectories(IEnumerable<ArtistSet> artistSetList)
        {
            var path = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription());
            if (Directory.Exists(path))
            {
                var allArtistDirectories = artistSetList.Select(x => x.GetCompressedNames());
                var artistPathNames = Directory.EnumerateDirectories(path).Select(p => Path.GetFileName(p));
                foreach (var ap in artistPathNames)
                {
                    log.Trace($"checking path {ap}");
                    if (!artistPathNames.Contains(ap, StringComparer.InvariantCultureIgnoreCase))
                    {
                        var fp = new DirectoryInfo(Path.Combine(path, ap));
                        RemoveInvalidFolders(new DirectoryInfo[] { fp });
                    }
                    //if (musicDb.Artists.SingleOrDefault(x => x.Name.ToLower() == ap.ToLower()) == null)
                    //{
                    //    var fp = new DirectoryInfo(Path.Combine(path, ap));
                    //    RemoveInvalidFolders(new DirectoryInfo[] { fp });
                    //    //log.Information($"{fp} deleted");
                    //}
                }
            }
        }
        public override (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateMovements(Performance performance, DirectoryInfo performanceDirectory)
        {
            var existingFiles = performanceDirectory.EnumerateFiles();//.Select(fi => Path.GetFileNameWithoutExtension(fi.Name));
            var compressedNames = performance.Movements // work.Tracks
                .Where(w => !string.IsNullOrWhiteSpace(w.CompressedName))
                .Select(x => x.CompressedName);
            var validFiles = existingFiles.Where(f => compressedNames.Contains(Path.GetFileNameWithoutExtension(f.Name), StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, new FileInfoComparer());//, StringComparer.InvariantCultureIgnoreCase);
            foreach (var file in validFiles)
            {
                var movement = performance.Movements.Single(x => x.CompressedName == Path.GetFileNameWithoutExtension(file.Name));
                log.Trace($"Work {movement.Work.Name}, valid file {file} ({movement.Title})");
            }
            foreach (var file in invalidFiles)
            {
                log.Information($"{performanceDirectory.FullName}, {performance.GetAllPerformersCSV()}, invalid file {file}");
            }
            return (validFiles.Select(x => x.FullName), invalidFiles.Select(x => x.FullName));
        }
        public override (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidatePerformanceFolders(DirectoryInfo compressedNameFolder, IEnumerable<PerformanceTuple> performanceTuples)
        {
            var existingFolders = compressedNameFolder.EnumerateDirectories();
            var fullNames = performanceTuples
                .Where(x => !string.IsNullOrWhiteSpace(x.Performance.CompressedName))
                .Select(x => x.Performance.CompressedName);
            var validFolders = existingFolders.Where(f => fullNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in invalidFolders)
            {
                log.Information($"invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }
        public override (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory)
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

        #endregion Public Methods

        #region Protected Methods

        protected string GetCompressedName(ArtistSet artistSet)
        {
            string MakeUnique(string name)
            {
                var exists = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower() /*string.Compare(x.CompressedName, name, true) == 0*/) != null;
                if (exists)
                {
                    int i = 1;
                    bool done = false;
                    var baseName = name;
                    do
                    {
                        name = $"{baseName}{(++i).ToString()}";
                        done = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower()/* string.Compare(x.CompressedName, name, true) == 0*/) == null;
                    } while (!done);
                }
                return name;
            }
            if (string.IsNullOrWhiteSpace(artistSet.GetCompressedNames()))
            {
                try
                {
                    foreach (var artist in artistSet.Artists)
                    {
                        if (artist.CompressedName == null)
                        {
                            var artistName = safeChars.Replace(artist.Name, string.Empty);
                            var parts = artistName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                static string GetInitial(string name)
                                {
                                    return name.Substring(0, 1).ToUpper();
                                }
                                var leadParts = parts.Take(parts.Length - 1);
                                var initials = string.Join("", leadParts.Select(x => GetInitial(x)));
                                artistName = $"{initials}{parts.Last()}";
                            }
                            artistName = MakeUnique(artistName);
                            artist.CompressedName = artistName;
                            musicDb.SaveChanges();
                            log.Information($"{artist.Name} -> {artist.CompressedName}");
                        }
                    }
                }
                catch (Exception)
                {
                    Debugger.Break();
                    throw;
                }
            }
            return artistSet.GetCompressedNames();
        }
        protected string GetNextUniqueName(string initialLetter, IEnumerable<string> existingNames)
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

        #endregion Protected Methods

        #region Private Methods

        private string GetCompressedName(Performance performance, IEnumerable<Performance> allPerformances)
        {
            if (string.IsNullOrWhiteSpace(performance.CompressedName))
            {
                var existingNames = allPerformances
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                performance.CompressedName = GetNextUniqueName("P", existingNames);
                musicDb.SaveChanges();
            }
            return performance.CompressedName;
        }

        #endregion Public Constructors
        #region Public Methods

        #endregion Public Methods

        #region Private Methods

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
}
