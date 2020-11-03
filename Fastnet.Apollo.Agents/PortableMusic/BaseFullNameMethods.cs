using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Apollo.Agents
{
    public abstract class BaseFullNameMethods : BaseFolderMethods
    {
        #region Public Constructors

        public BaseFullNameMethods(MusicStyles style, PortabilityConfiguration portConfig) : base(style, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        public override DirectoryInfo GetArtistDirectory(ArtistSet artistSet)
        {
            //var setName = GetCompressedName(artistSet);
            //var setPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), setName);
            //var compressedNames = GetDirectoryInfo(setPath);
            var setName = artistSet.GetNames();
            var setPath = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription(), setName);
            return GetDirectoryInfo(setPath);
            //return (fullNames, compressedNames);
        }
        public override DirectoryInfo GetArtistDirectory(Artist artist)
        {
            var fullPath = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription(), artist.Name);
            return GetDirectoryInfo(fullPath);
        }
        //public override string GetPerformanceName(PerformanceTuple tuple)
        //{
        //    return tuple.Name;
        //}
        public override string GetPerformanceName(PerformanceTuple tuple, IEnumerable<Performance> allPerformances = null)
        {
            return tuple.Name;
        }

        #endregion Public Constructors

        #region Public Methods

        public override string GetTrackFileName(Track movement, IEnumerable<Track> tracklist)
        {
            return $"{movement.MovementNumber:#00} {GetPathSafeName(movement.Title)}";
        }
        public override void RemoveInvalidArtistDirectories(IEnumerable<ArtistSet> artistSetList)
        {
            var path = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription());
            if (Directory.Exists(path))
            {
                var allArtistDirectories = artistSetList.Select(x => this.GetArtistDirectory(x))
                    .Select(x => Path.GetFileName(x.FullName));
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
            var names = performance.Movements
                .Select(x => $"{x.MovementNumber:#00} {GetPathSafeName(x.Title)}");
            var validFiles = existingFiles.Where(f => names.Contains(Path.GetFileNameWithoutExtension(f.Name), StringComparer.InvariantCultureIgnoreCase));
            var invalidFiles = existingFiles.Except(validFiles, new FileInfoComparer());//, StringComparer.InvariantCultureIgnoreCase);
            return (validFiles.Select(x => x.FullName), invalidFiles.Select(x => x.FullName));
        }
        public override (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidatePerformanceFolders(/*Artist artist,*/
            DirectoryInfo baseDirectory, IEnumerable<PerformanceTuple> performanceTuples)
        {
            var existingFolders = baseDirectory.EnumerateDirectories();
            var fullNames = performanceTuples.Select(x => GetPathSafeName(x.Name));
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
            var workNames = works
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .Select(x => GetPathSafeName(x.Name));                
            var validFolders = existingFolders.Where(f => workNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
            var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
            foreach (var di in validFolders)
            {
                //var work = works.Single(x => x.Name == di.Name);
                log.Trace($"Artist {artistSet.GetNames()}, valid folder {di.Name} ");
            }
            foreach (var di in invalidFolders)
            {
                log.Information($"{artistSet.ToString()} Artist {artistSet.GetNames()}, invalid folder {di.Name}");
            }
            return (validFolders, invalidFolders);
        }

        #endregion Public Methods
    }
}
