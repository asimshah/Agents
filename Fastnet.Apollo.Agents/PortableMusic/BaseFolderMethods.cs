using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Apollo.Agents
{
    public abstract class BaseFolderMethods
    {
        #region Protected Fields

        protected readonly ILogger log;
        protected readonly MusicStyles musicStyle;
        protected readonly PortabilityConfiguration portabilityConfiguration;
        protected MusicDb musicDb;

        #endregion Protected Fields

        #region Protected Constructors

        protected BaseFolderMethods(MusicStyles style, PortabilityConfiguration portConfig)
        {
            log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            this.musicStyle = style;
            this.portabilityConfiguration = portConfig;
        }

        #endregion Protected Constructors

        #region Public Methods

        public void ClearModifiedDirectories(IEnumerable<ArtistSet> artistSets)
        {
            foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
            {
                log.Debug($"processing artist {artistSet.GetNames()}");
                var artistDirectory = GetArtistDirectory(artistSet);
                if (artistDirectory.Exists)
                {
                    var lastModified = artistSet.Artists.Max(x => x.LastModified.UtcDateTime);
                    if (artistDirectory.LastWriteTimeUtc <= lastModified)
                    {
                        log.Debug($"{artistSet.GetNames()} db record(s) modified since last write to porting folder");
                        artistDirectory.Clear();
                        log.Information($"{artistSet.GetNames()} previous full names porting folder(s) cleared");
                    }
                }
            }
        }
        public abstract DirectoryInfo GetArtistDirectory(ArtistSet artistSet);
        public abstract DirectoryInfo GetArtistDirectory(Artist artist);
        public abstract string GetPerformanceName(PerformanceTuple tuple, IEnumerable<Performance> allPerformances = null);
        public abstract string GetTrackFileName(Track track, IEnumerable<Track> tracklist);
        public abstract void RemoveInvalidArtistDirectories(IEnumerable<ArtistSet> artistSetList);
        public void SetDb(MusicDb db)
        {
            this.musicDb = db;
        }
        public abstract (IEnumerable<string> validFiles, IEnumerable<string> invalidFiles) ValidateMovements(Performance performance, DirectoryInfo performanceDirectory);
        public abstract (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidatePerformanceFolders(
            DirectoryInfo fullNameDirectory, IEnumerable<PerformanceTuple> performanceTuples);
        public abstract (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateWorkFolders(ArtistSet artistSet, IEnumerable<Work> works, DirectoryInfo artistDirectory);

        #endregion Public Methods

        #region Protected Methods

        protected DirectoryInfo GetDirectoryInfo(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return new DirectoryInfo(path);
        }
        protected string GetPathSafeName(string name)
        {
            name = name.Replace("\"", "'");
            if(name.EndsWith("."))
            {
                name = name.Substring(0, name.Length - 1);
            }
            return name.GetPathSafeString();// string.Join("", name.Split(Path.GetInvalidFileNameChars()));
        }
        public void RemoveInvalidFolders(IEnumerable<DirectoryInfo> invalidFolders)
        {
            foreach (var di in invalidFolders)
            {
                di.Clear();
                di.Delete(true);
                log.Information($"{di.FullName} deleted");
            }
        }

        #endregion Protected Methods
    }
}
