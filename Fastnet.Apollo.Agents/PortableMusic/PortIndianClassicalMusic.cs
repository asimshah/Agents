using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Fastnet.Core;
using System.Linq;
using System.Collections.Generic;
using Fastnet.Music.TagLib;

namespace Fastnet.Apollo.Agents
{
    public class ICMCompressedNames : BaseCompressedNameMethods
    {
        #region Public Constructors

        public ICMCompressedNames(PortabilityConfiguration portConfig) : base(MusicStyles.IndianClassical, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        //public override string GetTrackFileName(Track track, IEnumerable<Track> tracklist)
        //{
        //    throw new NotSupportedException();
        //}

        #endregion Public Methods
    }

    public class ICMFullNames : BaseFullNameMethods
    {
        #region Public Constructors

        public ICMFullNames(PortabilityConfiguration portConfig) : base(MusicStyles.IndianClassical, portConfig)
        {
        }

        #endregion Public Constructors

        #region Public Methods

        //public override string GetTrackFileName(Track track, IEnumerable<Track> tracklist)
        //{
        //    throw new NotSupportedException();
        //}

        #endregion Public Methods
    }
    public abstract class PortIndianClassicalMusic<T> : PortMusic<T> where T : BaseFolderMethods
    {
        #region Private Fields

        private ArtistSet currentArtistSet;
        private Performance currentPerformance;
        private IEnumerable<Performance> currentPerformances;
        private Raga currentRaga;

        #endregion Private Fields

        #region Public Constructors

        public PortIndianClassicalMusic(T folderNamer, MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(folderNamer, musicOptions, MusicStyles.IndianClassical, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors

        #region Internal Methods

        //private IEnumerable<ArtistSetRagaPerformance> GetPerformancesByRagaAndArtists()
        //{
        //    return musicDb.RagaPerformances
        //        .AsEnumerable()
        //        .GroupBy(x => x.Performance)
        //        //.Select(g => (performance: g.Key, raga: g.Select(r => r.Raga).Distinct().Single(), artists: g.Select(a => a.Artist).OrderBy(a => a.Reputation).AsEnumerable()));
        //        .Select(g => new ArtistSetRagaPerformance { Performance = g.Key, Raga = g.Select(r => r.Raga).Distinct().Single(), ArtistSet = new ArtistSet(g.Select(z => z.Artist)) });
        //}
        //protected override void ProcessArtist(Artist artist, DirectoryInfo artistDirectory)
        //{
        //    throw new InvalidOperationException($"method not applicable to {musicStyle}");
        //}
        internal override CopiedTags LoadTags(Track track)
        {
            try
            {
                var artists = currentArtistSet;
                var raga = currentRaga;
                var performanceCount = currentPerformances.Count();
                var ct = new CopiedTags
                {
                    Performers = currentArtistSet.Artists.Select(x => x.Name).ToArray(),
                    Album = currentRaga.Name,
                    Track = (uint)track.MovementNumber,
                    Title = track.Title,
                    Genres = new string[] { this.musicStyle.ToDescription() }
                };
                SetPicture(ct, track);
                if (performanceCount > 1)
                {
                    var index = currentPerformances.ToList().IndexOf(currentPerformance);
                    ct.Album = $"{raga.Name} ({index + 1})";
                }
                return ct;
            }
            catch (Exception xe)
            {
                log.Error(xe, $"Failed to create tags for {track.Work.Name}, [{track.ToIdent()}] {track.Title})");
                throw;
            }
        }

        #endregion Internal Methods

        #region Protected Methods

        protected Task ProcessArtistSet(ArtistSet artistSet, DirectoryInfo artistDirectory/*, DirectoryInfo compressedNamePath*/)
        {
            log.Debug($"processing artist {artistSet.GetNames()}");
            currentArtistSet = artistSet; // used in LoadTags()
            var performancesOfAllRagas = musicDb.GetRagaPerformancesForArtistSet(currentArtistSet);
            var allPerformances = performancesOfAllRagas.Select(x => x.Performance);
            var allPerformanceTuples = allPerformances.Select(x => new PerformanceTuple { Performance = x, Name = GetPerformanceFullName(x, allPerformances) });
            var allRagas = performancesOfAllRagas.Select(x => x.Raga).Distinct();
            foreach (var raga in allRagas)
            {
                currentRaga = raga; // used in LoadTags()
                currentPerformances = performancesOfAllRagas.Where(l => l.Raga == raga).Select(l => l.Performance).OrderBy(x => x.GetAllPerformersCSV());
                var performanceTuples = currentPerformances
                    .Select(x => new PerformanceTuple { Performance = x, Name = GetPerformanceFullName(x, currentPerformances) })
                    .OrderBy(x => x.Name);
                (_, var invalidFullNameFolders) = folderNamer.ValidatePerformanceFolders(artistDirectory, allPerformanceTuples);
                folderNamer.RemoveInvalidFolders(invalidFullNameFolders);
                foreach (var tuple in performanceTuples)
                {
                    currentPerformance = tuple.Performance;
                    //PortPerformance(artistDirectory, tuple, currentPerformances);
                    PortPerformance(artistDirectory, tuple, allPerformances);
                }
            }
            return Task.FromResult(true);
        }
        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
            {
                var artistDirectory = folderNamer.GetArtistDirectory(artistSet);
                ProcessArtistSet(artistSet, artistDirectory);
            }
            return Task.CompletedTask;
        }

        #endregion Protected Methods

        #region Private Methods

        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateArtistSets(IEnumerable<ArtistSet> artistSets)
        //{
        //    var path = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription());
        //    var validFolders = artistSets.Select(x => GetArtistSetDirectory(x).FullName);
        //    if (Directory.Exists(path))
        //    {
        //        var existingFolders = Directory.EnumerateDirectories(path);
        //        var invalidFolders = existingFolders.Except(validFolders, StringComparer.CurrentCultureIgnoreCase);
        //        foreach (var di in validFolders)
        //        {
        //            log.Trace($"valid folder {di}");
        //        }
        //        foreach (var di in invalidFolders)
        //        {
        //            log.Information($"invalid folder {di}");
        //        }
        //        return (validFolders.Select(x => new DirectoryInfo(x)), invalidFolders.Select(x => new DirectoryInfo(x)));
        //    }
        //    else
        //    {
        //        Directory.CreateDirectory(path);
        //        log.Information($"{path} created");
        //        return (validFolders.Select(x => new DirectoryInfo(x)), Enumerable.Empty<DirectoryInfo>());
        //    }
        //}
        //private DirectoryInfo GetArtistSetDirectory(ArtistSet artistSet)
        //{
        //    var setName = string.Join("-", artistSet.Artists.Select(x => GetCompressedName(x)));
        //    var artistPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), setName);
        //    return GetDirectoryInfo(artistPath);
        //}
        //private string GetCompressedName(Performance performance, IEnumerable<ArtistSetRagaPerformance> currentRagaPerformances)
        //{
        //    if (string.IsNullOrWhiteSpace(performance.CompressedName))
        //    {
        //        var existingNames = currentRagaPerformances.Where(x => x.Raga.Id == currentRaga.Id).Select(x => x.Performance)
        //            .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
        //            .Select(x => x.CompressedName);
        //        performance.CompressedName = GetNextUniqueName("P", existingNames);
        //        musicDb.SaveChanges();
        //    }
        //    return performance.CompressedName;
        //}
        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidatePerformances(IEnumerable<Performance> performances, DirectoryInfo ragaDirectory)
        //{
        //    var existingFolders = ragaDirectory.EnumerateDirectories();
        //    var compressedNames = performances
        //        .Where(p => !string.IsNullOrWhiteSpace(p.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        log.Trace($"{ragaDirectory.FullName}, valid folder {di.Name} ");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"{ragaDirectory.FullName}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}

        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateRagas(/*IEnumerable<Artist> artistSet,*/ IEnumerable<Raga> ragas, DirectoryInfo setDirectoryInfo)
        //{
        //    var existingFolders = setDirectoryInfo.EnumerateDirectories();
        //    var compressedNames = ragas.Where(r => !string.IsNullOrWhiteSpace(r.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var raga = ragas.Single(x => x.CompressedName == di.Name);
        //        log.Trace($"{setDirectoryInfo.FullName}, {raga.DisplayName}, valid folder {di.Name}");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"{setDirectoryInfo.FullName}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        //private string GetCompressedName(Raga raga)
        //{
        //    if (string.IsNullOrWhiteSpace(raga.CompressedName))
        //    {
        //        var existingNames = musicDb.Ragas
        //            .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
        //            .Select(x => x.CompressedName);
        //        raga.CompressedName = GetNextUniqueName("R", existingNames);
        //        musicDb.SaveChanges();
        //    }
        //    return raga.CompressedName;
        //}
        private string GetPerformanceFullName(Performance performance, IEnumerable<Performance> allPerformances)
        {
            var raga = performance.GetRaga();
            var name = raga.DisplayName;
            var allPerformanceOfThisRaga = allPerformances.Where(x => x.GetRaga() == raga);
            if (allPerformanceOfThisRaga.Count() > 1)
            {
                var index = allPerformanceOfThisRaga.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(performance);
                name = $"{raga.DisplayName} ({index + 1})";
            }
            return name;
        }

        #endregion Private Methods
    }

    public class PortIndianClassicalMusicCN : PortIndianClassicalMusic<ICMCompressedNames>
    {
        #region Public Constructors

        public PortIndianClassicalMusicCN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(new ICMCompressedNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors

        //protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        //{
        //    throw new NotImplementedException();
        //}

        //internal override CopiedTags LoadTags(Track track)
        //{
        //    throw new NotImplementedException();
        //}
    }

    public class PortIndianClassicalMusicFN : PortIndianClassicalMusic<ICMFullNames>
    {
        #region Public Constructors

        public PortIndianClassicalMusicFN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(new ICMFullNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors
    }
}
