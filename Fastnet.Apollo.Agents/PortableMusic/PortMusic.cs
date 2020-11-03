using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.TagLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public abstract class PortMusic<T> : IPipelineTask where T : BaseFolderMethods
    {

        #region Protected Fields

        protected readonly T folderNamer;
        protected readonly ILogger log;
        protected readonly MusicOptions musicOptions;
        protected readonly MusicStyles musicStyle;
        protected MusicDb musicDb;
        protected PortabilityConfiguration portabilityConfiguration;
        #endregion Protected Fields

        #region Private Fields

        private readonly string connectionString;

        #endregion Private Fields

        #region Public Properties

        public TaskMethod ExecuteAsync => DoTask;
        public string Name => $"{this.GetType().Name}-{musicStyle.ToString()}";

        #endregion Public Properties

        #region Public Constructors

        public PortMusic(T folderNamer, MusicOptions musicOptions, MusicStyles style,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            PortabilityConfiguration portConfig)
        {
            this.folderNamer = folderNamer;
            this.musicOptions = musicOptions;
            this.musicStyle = style;
            this.portabilityConfiguration = portConfig;
            this.log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            connectionString = environment.LocaliseConnectionString(configuration.GetConnectionString("MusicDb"));
        }

        #endregion Public Constructors

        #region Internal Methods

        internal abstract CopiedTags LoadTags(Track track);
        internal virtual void SetPicture(CopiedTags ct, Track track)
        {
            if (track.Work.Cover != null)
            {
                ct.Pictures = new[] { new Picture(track.Work.Cover.Data) };
            }
            else
            {
                log.Warning($"{track.Work.ToIdent()} {track.Work.Name} does not have a a cover picture");
            }
        }

        #endregion Internal Methods

        #region Protected Methods

        protected void CopyTrack(Track track, DirectoryInfo dir, string musicFilename)
        {
            var flacFile = track.MusicFiles.FirstOrDefault(t => t.Encoding == EncodingType.flac);
            if (flacFile == null)
            {
                var mp3File = track.MusicFiles.Where(t => t.Encoding == EncodingType.mp3)
                    .OrderByDescending(t => t.AverageBitRate)
                    .FirstOrDefault();
                if (mp3File == null)
                {
                    log.Warning($"No supported encoding found for artist(s) {track.Work.GetArtistNames()}, album {track.Work.Name} track {track.Title}");
                }
                else
                {
                    CopyMp3(mp3File, dir, musicFilename);
                }
            }
            else
            {
                CopyVBR(flacFile, dir, musicFilename);
            }
        }
        protected async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            IEnumerable<ArtistSet> getSetsUsingWorks()
            {
                return musicDb.Works
                    .Where(w => w.StyleId == this.musicStyle)
                    .AsEnumerable()
                    .Where(w => !w.Artists.Any(a => a.Type == ArtistType.Various))
                    .Select(x => new ArtistSet(x.Artists)).Distinct(new ArtistSetComparer());
            }
            IEnumerable<ArtistSet> getSetsUsingCompositions()
            {
                return musicDb.Compositions
                    .Where(c => c.Artist.ArtistStyles.Select(x => x.StyleId).Contains(this.musicStyle)).Select(x => x.Artist)
                    .AsEnumerable()
                    .Where(w => w.Type != ArtistType.Various)
                    .Select(x => new ArtistSet(x)).Distinct(new ArtistSetComparer());
            }
            using (musicDb = new MusicDb(connectionString))
            {
                log.Information("started");
                this.folderNamer.SetDb(musicDb);
                var artistSetList = this.musicStyle == MusicStyles.WesternClassical ? getSetsUsingCompositions() : getSetsUsingWorks();
                folderNamer.RemoveInvalidArtistDirectories(artistSetList);
                folderNamer.ClearModifiedDirectories(artistSetList);
                await StartAsync(artistSetList);
            }
            return (ITaskState)null;
        }
        //protected (DirectoryInfo fullnames, DirectoryInfo compressedNames) GetArtistDirectories(ArtistSet artistSet)
        //{
        //    var setName = GetCompressedName(artistSet);
        //    var setPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), setName);
        //    var compressedNames = GetDirectoryInfo(setPath);
        //    setName = artistSet.GetNames();
        //    setPath = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription(), setName);
        //    var fullNames = GetDirectoryInfo(setPath);
        //    return (fullNames, compressedNames);
        //}
        //protected (DirectoryInfo fullNamePath, DirectoryInfo compressedNamePath) GetArtistDirectories(Artist artist)
        //{
        //    var compressedPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), artist.CompressedName);
        //    var fullPath = Path.Combine(this.portabilityConfiguration.FullNamesRoot, this.musicStyle.ToDescription(), artist.Name);
        //    return (GetDirectoryInfo(fullPath), GetDirectoryInfo(compressedPath));
        //}
        //protected abstract string GetCompressedName(Performance performance);
        //protected string GetCompressedName(Track track, IEnumerable<Track> trackList)
        //{
        //    if (string.IsNullOrWhiteSpace(track.CompressedName))
        //    {
        //        var existingNames = trackList
        //            .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
        //            .Select(x => x.CompressedName);
        //        track.CompressedName = GetNextUniqueName("M", existingNames);

        //        musicDb.SaveChanges();
        //    }
        //    return track.CompressedName;
        //}
        //protected string GetCompressedName(ArtistSet artistSet)
        //{
        //    string MakeUnique(string name)
        //    {
        //        var exists = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower() /*string.Compare(x.CompressedName, name, true) == 0*/) != null;
        //        if (exists)
        //        {
        //            int i = 1;
        //            bool done = false;
        //            var baseName = name;
        //            do
        //            {
        //                name = $"{baseName}{(++i).ToString()}";
        //                done = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower()/* string.Compare(x.CompressedName, name, true) == 0*/) == null;
        //            } while (!done);
        //        }
        //        return name;
        //    }
        //    if (string.IsNullOrWhiteSpace(artistSet.GetCompressedNames()))
        //    {
        //        try
        //        {
        //            foreach (var artist in artistSet.Artists)
        //            {
        //                if (artist.CompressedName == null)
        //                {
        //                    var artistName = safeChars.Replace(artist.Name, string.Empty);
        //                    var parts = artistName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //                    if (parts.Length > 1)
        //                    {
        //                        static string GetInitial(string name)
        //                        {
        //                            return name.Substring(0, 1).ToUpper();
        //                        }
        //                        var leadParts = parts.Take(parts.Length - 1);
        //                        var initials = string.Join("", leadParts.Select(x => GetInitial(x)));
        //                        artistName = $"{initials}{parts.Last()}";
        //                    }
        //                    artistName = MakeUnique(artistName);
        //                    artist.CompressedName = artistName;
        //                    musicDb.SaveChanges();
        //                    log.Information($"{artist.Name} -> {artist.CompressedName}");
        //                }
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            Debugger.Break();
        //            throw;
        //        }
        //    }
        //    return artistSet.GetCompressedNames();
        //}
        //protected string GetCompressedName(Artist artist)
        //{
        //    string MakeUnique(string name)
        //    {
        //        var exists = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower() /*string.Compare(x.CompressedName, name, true) == 0*/) != null;
        //        if (exists)
        //        {
        //            int i = 1;
        //            bool done = false;
        //            var baseName = name;
        //            do
        //            {
        //                name = $"{baseName}{(++i).ToString()}";
        //                done = musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == name.ToLower()/* string.Compare(x.CompressedName, name, true) == 0*/) == null;
        //            } while (!done);
        //        }
        //        return name;
        //    }
        //    if (string.IsNullOrWhiteSpace(artist.CompressedName))
        //    {
        //        try
        //        {
        //            var artistName = safeChars.Replace(artist.Name, string.Empty);
        //            var parts = artistName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //            if (parts.Length > 1)
        //            {
        //                static string GetInitial(string name)
        //                {
        //                    return name.Substring(0, 1).ToUpper();
        //                }
        //                var leadParts = parts.Take(parts.Length - 1);
        //                var initials = string.Join("", leadParts.Select(x => GetInitial(x)));
        //                artistName = $"{initials}{parts.Last()}";
        //            }
        //            artistName = MakeUnique(artistName);
        //            artist.CompressedName = artistName;
        //            musicDb.SaveChanges();
        //            log.Information($"{artist.Name} -> {artist.CompressedName}");
        //        }
        //        catch (Exception)
        //        {
        //            Debugger.Break();
        //            throw;
        //        }
        //    }
        //    return artist.CompressedName;
        //}
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
            return string.Join("", name.Split(Path.GetInvalidFileNameChars()));
        }
        protected void PortPerformance(DirectoryInfo parentDirectory, PerformanceTuple tuple, IEnumerable<Performance> allPerformances = null)
        {
            string performanceName = folderNamer.GetPerformanceName(tuple, allPerformances);
            var performanceDirectory = GetDirectoryInfo(Path.Combine(parentDirectory.FullName, GetPathSafeName(performanceName)));
            var (_, invalidfiles) = folderNamer.ValidateMovements(tuple.Performance, performanceDirectory);// ValidateMovementsUsingCompressedNames(performance, performanceDirectory);
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
            var trackList = tuple.Performance.Movements.ToArray();
            foreach (var movement in trackList)
            {
                //var filename = GetCompressedName(movement, trackList);
                var filename = folderNamer.GetTrackFileName(movement, trackList);
                CopyTrack(movement, performanceDirectory, filename);
            }
        }
        protected abstract Task StartAsync(IEnumerable<ArtistSet> artistSets);
        #endregion Protected Methods

        #region Private Methods

        private bool CompareTags(Track src, string destination)
        {
            static bool comparePictures(IPicture[] src, IPicture[] dest)
            {
                if (src == null && dest == null) return true;
                if (dest == null && src != null && src.Length == 0) return true;
                if (src == null || dest == null) return false;
                if (src.Length == 0 && dest.Length == 0) return true;
                if (src.Length == 0 || dest.Length == 0) return false;
                return dest[0].Data.Data.SequenceEqual(src[0].Data.Data);
            }
            try
            {
                var srcTags = LoadTags(src);
                var destfile = Music.TagLib.File.Create(destination);
                if (destfile.Tag.Performers[0] != srcTags.Performers[0]
                    || destfile.Tag.Album != srcTags.Album
                    || destfile.Tag.Track != srcTags.Track
                    || destfile.Tag.Title != srcTags.Title
                    || destfile.Tag.Genres[0] != srcTags.Genres[0]
                    || !comparePictures(destfile.Tag.Pictures, srcTags.Pictures)
                    )
                {
                    return false;
                }
                return true;
            }
            catch (Exception xe)
            {
                log.Error(xe, $"{src.ToIdent()} {src.Title}, destination {destination}");
                throw;
            }
        }
        private void CopyFile(Track track, string source, string destination)
        {

            if (System.IO.File.Exists(source))
            {
                if (System.IO.File.Exists(destination))
                {
                    try
                    {
                        var match = CompareTags(track, destination);
                        var src = new FileInfo(source);
                        var dest = new FileInfo(destination);
                        if (match == false || src.LastWriteTimeUtc > dest.LastWriteTimeUtc /*|| force*/)
                        {
                            System.IO.File.Delete(destination);
                            log.Information($"{destination} deleted: {(match == false ? "tags did not match" : "original modified later")}");
                        }
                        else
                        {
                            log.Trace($"{destination} [{track.Work.Name}:{track.Title}] retained as tags match and original is same datetime or earlier");
                        }
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe, $"copy {source} to {destination} failed");
                    }
                }
                if (!System.IO.File.Exists(destination))
                {
                    try
                    {
                        System.IO.File.Copy(source, destination);
                        var tags = LoadTags(track);
                        tags.WriteTags(destination);
                        log.Information($"{destination} created [{track.Work.Name}:{track.Title}]");
                        var effectiveLength = destination.StartsWith(this.portabilityConfiguration.FullNamesRoot) ?
                        destination.Length - this.portabilityConfiguration.FullNamesRoot.Length : destination.Length - this.portabilityConfiguration.CompressedNamesRoot.Length;
                        if (effectiveLength > 200)
                        {
                            log.Warning($"{destination} is ${destination.Length} long - may be too long");
                        }
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe, $"copy {source} to {destination} failed");
                    }
                }
            }
            else
            {
                log.Error($"Music file {source} not found");
            }
        }
        private void CopyMp3(MusicFile mf, DirectoryInfo dir, string musicFileName)
        {
            var mp3File = mf.File;
            var targetFile = Path.Combine(dir.FullName, $"{musicFileName}.mp3");
            CopyFile(mf.Track, mp3File, targetFile);
        }
        private void CopyVBR(MusicFile mf, DirectoryInfo dir, string musicFileName)
        {
            var generatedSource = musicOptions.Sources.Single(x => x.IsGenerated);
            var sourceRoot = mf.DiskRoot;
            var baseFileName = mf.File.Substring(mf.DiskRoot.Length + 1);
            var relativeNameWithoutExtension = Path.Combine(Path.GetDirectoryName(baseFileName), Path.GetFileNameWithoutExtension(baseFileName));
            var vbrFile = Path.Combine(generatedSource.DiskRoot, relativeNameWithoutExtension + ".mp3");
            var targetFile = Path.Combine(dir.FullName, $"{musicFileName}.mp3");
            if (System.IO.File.Exists(vbrFile))
            {
                CopyFile(mf.Track, vbrFile, targetFile);
            }
            else
            {
                log.Warning($"{vbrFile} not found");
            }
        }

        #endregion Private Methods
    }

    //public enum NameStyle
    //{
    //    Full,
    //    Compressed
    //}
}
