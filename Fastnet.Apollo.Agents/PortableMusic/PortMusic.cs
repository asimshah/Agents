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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    internal class CopiedTags
    {
        public string[] Performers { get; set; }
        public string Album { get; set; }
        public uint Track { get; set; }
        public string Title { get; set; }
        public Picture[] Pictures { get; set; }
        public string[] Genres { get; set; }
        public void WriteTags(string targetFile)
        {
            var file = Music.TagLib.File.Create(targetFile);
            file.Tag.Clear();
            file.Tag.Performers = Performers;// new string[] { track.Work.Artist.Name };
            file.Tag.Album = Album; // track.Performance.Composition.Name;
            file.Tag.Track = Track;// (uint)track.Number;
            file.Tag.Title = Title;// track.Title;
            file.Tag.Pictures = Pictures;// new[] { new Picture(track.Work.CoverData) };
            file.Tag.Genres = Genres;// new string[] { this.musicStyle.ToDescription() };
            file.Save();
        }
    }
    public abstract class PortMusic : IPipelineTask // StyleTask
    {
        private Regex safeChars = new Regex(@"[^\sa-zA-Z0-9\p{L}]", RegexOptions.IgnoreCase);
        //private bool noChanges = false;
        //private bool force = false;
        //private IEnumerable<RootMap> flacRoots;
        protected PortabilityConfiguration portabilityConfiguration;
        protected MusicDb musicDb;
        protected readonly ILogger log;
        private readonly string connectionString;
        protected readonly MusicStyles musicStyle;
        protected readonly MusicOptions musicOptions;
        public string Name => $"{this.GetType().Name}-{musicStyle.ToString()}";


        public TaskMethod ExecuteAsync => DoTask;
        //protected abstract Task StartAsync();
        public PortMusic(MusicOptions musicOptions, MusicStyles style,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            //IServiceProvider serviceProvider,
            PortabilityConfiguration portConfig)
        {
            this.musicOptions = musicOptions;
            this.musicStyle = style;
            this.portabilityConfiguration = portConfig;
            this.log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            connectionString = environment.LocaliseConnectionString(configuration.GetConnectionString("MusicDb"));
            //this.resamplingConfiguration = resamplingConfig.Value;
            //flacRoots = resamplingConfiguration.Maps;
        }
        protected async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            using (musicDb = new MusicDb(connectionString))
            {
                await StartAsync();
            }
            return (ITaskState)null;
        }
        protected abstract void ProcessArtist(Artist artist, DirectoryInfo artistDirectory);

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
                    log.Warning($"No supported encoding found for artist {track.Work.Artist.Name}, album {track.Work.Name} track {track.Title}");
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
        internal abstract CopiedTags LoadTags(Track track);
        protected string GetNextUniqueName(string initialLetter, IEnumerable<string> existingNames)
        {
            bool done = false;
            int i = 0;
            string name = null;
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
        protected DirectoryInfo GetDirectoryInfo(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return new DirectoryInfo(path);
        }
        protected string GetCompressedName(Track track, IEnumerable<Track> trackList)
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
        protected string GetPathSafeName(string name)
        {
            return string.Join("", name.Split(Path.GetInvalidFileNameChars()));
        }
        protected void RemoveInvalidFolders(IEnumerable<DirectoryInfo> invalidFolders)
        {
            foreach (var di in invalidFolders)
            {
                di.Delete(true);
                log.Information($"{di.FullName} deleted");
            }
        }
        private void RemoveInvalidArtistDirectories()
        {
            var path = Path.Combine(this.portabilityConfiguration.PortableLibraryRoot, this.musicStyle.ToDescription());
            if (Directory.Exists(path))
            {
                var artistPathNames = Directory.EnumerateDirectories(path).Select(p => Path.GetFileName(p));
                foreach (var ap in artistPathNames)
                {
                    //if (musicDb.Artists.SingleOrDefault(x => string.Compare(x.CompressedName, ap, true) == 0) == null)
                    if (musicDb.Artists.SingleOrDefault(x => x.CompressedName.ToLower() == ap.ToLower()) == null)
                    {
                        var fp = new DirectoryInfo(Path.Combine(path, ap));
                        RemoveInvalidFolders(new DirectoryInfo[] { fp });
                        //log.Information($"{fp} deleted");
                    }
                }
            }
        }
        private async Task StartAsync()
        {
            log.Information("started");
            RemoveInvalidArtistDirectories();
            var artists = await musicDb.Artists
                .Where(a => a.Type != ArtistType.Various && a.ArtistStyles.Select(x => x.StyleId).Contains(this.musicStyle))
                .ToArrayAsync();

            foreach (var artist in artists.OrderBy(x => x.Name))
            {
                DirectoryInfo artistDirectory = GetArtistDirectory(artist);
                if (artistDirectory.Exists)
                {
                    if (artistDirectory.LastWriteTimeUtc <= artist.LastModified.UtcDateTime)
                    {
                        log.Debug($"{artist.Name} db record modified since last write to porting folder");
                        artistDirectory.Clear();
                        log.Information($"{artist.Name} previous porting folder cleared");
                        //ClearContents(artistDirectory);
                    }

                }
                //ClearContents(artistDirectory);
                ProcessArtist(artist, artistDirectory);
            }
            return;
        }

        private DirectoryInfo GetArtistDirectory(Artist artist)
        {
            try
            {
                var artistName = GetCompressedName(artist);
                var artistPath = Path.Combine(this.portabilityConfiguration.PortableLibraryRoot, this.musicStyle.ToDescription(), artistName);
                return GetDirectoryInfo(artistPath);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }
        private string GetCompressedName(Artist artist)
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
            if (string.IsNullOrWhiteSpace(artist.CompressedName))
            {
                try
                {
                    var artistName = safeChars.Replace(artist.Name, string.Empty);
                    var parts = artistName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        string GetInitial(string name)
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
                catch (Exception)
                {
                    Debugger.Break();
                    throw;
                }
            }
            return artist.CompressedName;
        }
        private void CopyMp3(MusicFile mf, DirectoryInfo dir, string musicFileName)
        {

            var baseFileName = mf.File.Substring(mf.DiskRoot.Length + 1);
            var relativeNameWithoutExtension = Path.Combine(Path.GetDirectoryName(baseFileName), Path.GetFileNameWithoutExtension(baseFileName));
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
            //var vbrFile = Path.Combine(root.TargetRoot, relativeNameWithoutExtension + ".mp3");
            var vbrFile = Path.Combine(generatedSource.DiskRoot, relativeNameWithoutExtension + ".mp3");
            var targetFile = Path.Combine(dir.FullName, $"{musicFileName}.mp3");// GetTargetFilename(track);// Path.Combine(this.musicConfiguration.PortableLibraryRoot, this.musicStyle.ToDescription(), work.Artist.Name, work.Name, $"{track.Number.ToString("#00")} - {track.Title}.mp3");// Path.Combine(this.musicConfiguration.PortableLibraryRoot, relativeNameWithoutExtension + ".mp3");
            if (System.IO.File.Exists(vbrFile))
            {
                CopyFile(mf.Track, vbrFile, targetFile);
                //UpdateIdTags(work, track, targetFile);
            }
            else
            {
                log.Warning($"{vbrFile} not found");
            }
        }
        private void CopyFile(Track track, string source, string destination)
        {

            if (System.IO.File.Exists(source))
            {
                if (System.IO.File.Exists(destination))
                {
                    var match = CompareTags(track, destination);
                    var src = new FileInfo(source);
                    var dest = new FileInfo(destination);
                    if (match == false || src.LastWriteTimeUtc > dest.LastWriteTimeUtc /*|| force*/)
                    {
                        System.IO.File.Delete(destination);
                        log.Information($"{destination} deleted: {(match == false ? "tags did not match" : "original modified later")}");
                    }
                }
                if (!System.IO.File.Exists(destination))
                {
                    try
                    {
                        System.IO.File.Copy(source, destination);
                        var tags = LoadTags(track);
                        tags.WriteTags(destination);
                        log.Information($"{destination} created");
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
        private bool CompareTags(Track src, string destination)
        {
            var srcTags = LoadTags(src);
            var destfile = Music.TagLib.File.Create(destination);
            if (
                destfile.Tag.Performers[0] != srcTags.Performers[0]
                || destfile.Tag.Album != srcTags.Album
                || destfile.Tag.Track != srcTags.Track
                || destfile.Tag.Title != srcTags.Title
                || destfile.Tag.Genres[0] != srcTags.Genres[0]
                || !destfile.Tag.Pictures[0].Data.Data.SequenceEqual(srcTags.Pictures[0].Data.Data)

                )
            {
                return false;
            }
            return true;
        }
    }
    internal class DirectoryInfoComparer : IEqualityComparer<DirectoryInfo>
    {
        public bool Equals(DirectoryInfo x, DirectoryInfo y)
        {
            return x.FullName.Equals(y.FullName);
        }
        public int GetHashCode(DirectoryInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
