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
    public class PortIndianClassicalMusic : PortMusic
    {
        private ArtistSet currentArtistSet;
        //private IEnumerable<ArtistSetRagaPerformance> currentRagaPerformances;
        private Raga currentRaga;
        private IEnumerable<Performance> currentPerformances;
        private Performance currentPerformance;
        public PortIndianClassicalMusic(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig) : base(musicOptions, MusicStyles.IndianClassical, configuration, environment, portConfig)
        {
        }
        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            foreach (var artistSet in artistSets.OrderBy(x => x.GetNames()))
            {
                (var fullNamesDirectory, var compressedNamesDirectory) = GetArtistDirectories(artistSet);
                ProcessArtistSet(artistSet, fullNamesDirectory, compressedNamesDirectory);
            }
            return Task.CompletedTask;
        }
        protected Task ProcessArtistSet(ArtistSet artistSet, DirectoryInfo fullNamePath, DirectoryInfo compressedNamePath)
        {
            //log.Information("started");
            //var performancesByRagaAndArtists = musicDb.GetPerformancesByRagaAndArtists();// GetPerformancesByRagaAndArtists();
            // var artistSets = performancesByRagaAndArtists.Select(x => x.ArtistSet).Distinct(new ArtistSetComparer());
            //var (validFolders, invalidFolders) = ValidateArtistSets(artistSets);
            //RemoveInvalidFolders(invalidFolders);
            log.Debug($"processing artist {(string.Join(", ", artistSet.Artists.Select(a => a.Name)))}");
            currentArtistSet = artistSet; // used in LoadTags()
            var currentRagaPerformances = musicDb.GetRagaPerformancesForArtistSet(currentArtistSet);
            var artistSetDirectoryInfo = GetArtistSetDirectory(currentArtistSet);
            var ragas = currentRagaPerformances.Select(x => x.Raga).Distinct();// list.Select(x => x.raga);
                                                                               //(validFolders, invalidFolders) = ValidateRagas(/*artistSet,*/ ragas, artistSetDirectoryInfo);
                                                                               //RemoveInvalidFolders(invalidFolders);
            foreach (var raga in ragas)
            {
                currentRaga = raga; // used in LoadTags()
                currentPerformances = currentRagaPerformances.Where(l => l.Raga == raga).Select(l => l.Performance).OrderBy(x => x.GetAllPerformersCSV());
                var performanceTuples = currentPerformances
                    .Select(x => new PerformanceTuple { Performance = x, Name = GetPerformanceFullName(x, currentPerformances) })
                    .OrderBy(x => x.Name);
                (_, var invalidFullNameFolders) = ValidateFullNamePerformanceFolders( fullNamePath, performanceTuples);
                RemoveInvalidFolders(invalidFullNameFolders);
                (_, invalidFullNameFolders) = ValidateCompressedNamePerformanceFolders(compressedNamePath, performanceTuples);
                RemoveInvalidFolders(invalidFullNameFolders);
                foreach (var tuple in performanceTuples)
                {
                    currentPerformance = tuple.Performance;
                    PortPerformance(fullNamePath, tuple.Performance, tuple.Name, (m, tl) => { return m.Title; }, ValidateMovementsUsingFullNames);
                    PortPerformance(compressedNamePath, tuple.Performance, GetCompressedName(tuple.Performance, currentRagaPerformances), (m, tl) => GetCompressedName(m, tl), ValidateMovementsUsingCompressedNames);
                }
            }
            return Task.FromResult(true);
        }
        private string GetCompressedName(Performance performance, IEnumerable<ArtistSetRagaPerformance> currentRagaPerformances)
        {
            if (string.IsNullOrWhiteSpace(performance.CompressedName))
            {
                var existingNames = currentRagaPerformances.Where(x => x.Raga.Id == currentRaga.Id).Select(x => x.Performance)
                    .Where(x => !string.IsNullOrWhiteSpace(x.CompressedName))
                    .Select(x => x.CompressedName);
                performance.CompressedName = GetNextUniqueName("P", existingNames);
                musicDb.SaveChanges();
            }
            return performance.CompressedName;
        }
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
        private DirectoryInfo GetArtistSetDirectory(ArtistSet artistSet)
        {
            var setName = string.Join("-", artistSet.Artists.Select(x => GetCompressedName(x)));
            var artistPath = Path.Combine(this.portabilityConfiguration.CompressedNamesRoot, this.musicStyle.ToDescription(), setName);
            return GetDirectoryInfo(artistPath);
        }
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
            //var rpList = musicDb.RagaPerformances.Where(x => x.Performance == track.Performance);
            //var artists = currentRagaPerformances.Select(x => x.Artists).First().OrderBy(a => a.Reputation);
            try
            {
                var artists = currentArtistSet;// currentRagaPerformances.Select(x => x.ArtistSet.Artists).First();
                var raga = currentRaga;// currentRagaPerformances.Where(x => x.Performance == track.Performance).Select(x => x.Raga).Distinct().Single();
                var performanceCount = currentPerformances.Count();// currentRagaPerformances.Where(x => x.Raga == raga).Count();
                var ct = new CopiedTags
                {
                    Performers = currentArtistSet.Artists.Select(x => x.Name).ToArray(), // artists.Select(x => x.Name).ToArray(),// new string[] { track.Performance.Composition.Artist.Name },
                    Album = currentRaga.Name,// track.Performance.Composition.Name,
                    Track = (uint)track.MovementNumber,// track.Number,
                    Title = track.Title,
                    //Pictures = new[] { new Picture(track.Work.Cover.Data) },
                    Genres = new string[] { this.musicStyle.ToDescription() }
                };
                SetPicture(ct, track);
                if (performanceCount > 1)
                {
                    var index = currentPerformances.ToList().IndexOf(currentPerformance);// currentRagaPerformances.Where(x => x.Raga == raga).Select(x => x.Performance).OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(track.Performance);
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
            var name = raga.Name;
            if (allPerformances.Count() > 1)
            {
                var index = allPerformances.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(performance);
                name = $"{raga.Name} ({index + 1})";
            }
            return name;
        }
    }
}
