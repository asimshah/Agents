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
using System.Diagnostics;

namespace Fastnet.Apollo.Agents
{

    public class PortWesternClassicalMusic : PortMusic
    {
        public PortWesternClassicalMusic(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment,
            PortabilityConfiguration portConfig) : base(musicOptions, MusicStyles.WesternClassical, configuration, environment, portConfig)
        {
        }
        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            // ignore artistSets as WesternClassical does not handle multiple artists for one work
            // this is because is by composition and performance and compositions can only have one artist

            Debug.Assert(artistSets.All(x => x.Artists.Count() == 1));

            //var artists = musicDb.ArtistStyles.Where(x => x.StyleId == MusicStyles.WesternClassical)
            //    .Where(x => x.Artist.Type != ArtistType.Various)
            //    .Select(x => x.Artist);
            //var firstCount = artists.Count();
            //var secondCount = artistSets.Count();
            //var a1 = artists.ToArray();
            //var a2 = artistSets.ToArray();
            //Debug.Assert(artists.Count() == artistSets.Count());
            foreach(var artist in artistSets.SelectMany(x => x.Artists).OrderBy(x => x.Name))
            {
                (var fnp, var cnp) = GetArtistDirectories(artist);
                ProcessArtist(artist, fnp, cnp);
            }
            return Task.CompletedTask;
        }
        private  void ProcessArtist(Artist artist, DirectoryInfo fullNamePath, DirectoryInfo compressedNamePath)
        {
            //(_, var invalidFullNameFolders) = ValidateFullNameCompositions(artist, fullNamePath);
            //RemoveInvalidFolders(invalidFullNameFolders);
            //(_, var invalidCompressedNameFolders) = ValidateCompressedNameCompositions(artist, compressedNamePath);
            //RemoveInvalidFolders(invalidCompressedNameFolders);
            //var compositions = artist.Compositions.OrderBy(c => c.Name);
            //foreach (var composition in compositions)
            //{
            //    foreach(var item in composition.Performances.Select(x => new { Performance = x, Name = GetPerformanceFullName(x) })
            //        .OrderBy(x => x.Name))
            //    {

            //    }
            //    var compositionDirectory = GetDirectoryInfo(Path.Combine(compressedNamePath.FullName, GetCompressedName(composition)));
            //    (_, invalidCompressedNameFolders) = ValidateCompressedNamePerformances(composition, compositionDirectory);
            //    RemoveInvalidFolders(invalidCompressedNameFolders);
            //    foreach (var performance in composition.Performances)
            //    {
            //        PortPerformanceUsingCompressedNames(compositionDirectory, performance);
            //    }
            //}

            var performanceTuples = artist.Compositions.SelectMany(x => x.Performances)
                .Select(x => new PerformanceTuple { Performance = x, Name = GetPerformanceFullName(x) })
                .OrderBy(x => x.Name);
            (_, var invalidFullNameFolders) = ValidateFullNamePerformanceFolders(/*artist,*/ fullNamePath, performanceTuples);
            RemoveInvalidFolders(invalidFullNameFolders);
            (_,  invalidFullNameFolders) = ValidateCompressedNamePerformanceFolders(/*artist,*/ compressedNamePath, performanceTuples);
            RemoveInvalidFolders(invalidFullNameFolders);
            foreach(var tuple in performanceTuples)
            {
                PortPerformance(fullNamePath, tuple.Performance, tuple.Name, (m, tl) => { return $"{m.MovementNumber:#00} {GetPathSafeName(m.Title)}"; }, ValidateMovementsUsingFullNames);
                PortPerformance(compressedNamePath, tuple.Performance, GetCompressedName(tuple.Performance), (m, tl) => GetCompressedName(m, tl), ValidateMovementsUsingCompressedNames);
            }
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

        internal override CopiedTags LoadTags(Track track)
        {
            var ct = new CopiedTags
            {
                Performers = new string[] { track.Performance.Composition.Artist.Name },
                Album = track.Performance.Composition.Name,
                Track = (uint) track.MovementNumber,// track.Number,
                Title = track.Title,
                //Pictures = new[] { new Picture(track.Work.Cover.Data) },
                Genres = new string[] { this.musicStyle.ToDescription() }
            };
            SetPicture(ct, track);
            var perfomanceCount = track.Performance.Composition.Performances.Count();
            if(perfomanceCount > 1)
            {
                var index = track.Performance.Composition.Performances.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(track.Performance);
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

        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateFullNameCompositions(Artist artist, DirectoryInfo artistDirectory)
        //{
        //    var existingFolders = artistDirectory.EnumerateDirectories();
        //    var fullNames = artist.Compositions
        //        .Where(c => !string.IsNullOrWhiteSpace(c.Name))
        //        .Select(x => x.Name);
        //    var validFolders = existingFolders.Where(f => fullNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var composition = artist.Compositions.Single(x => x.Name == di.Name);
        //        log.Trace($"Artist {artist.Name}, valid folder {di.Name} ({composition.Name})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Artist {artist.Name}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateCompressedNameCompositions(Artist artist, DirectoryInfo artistDirectory)
        //{
        //    var existingFolders = artistDirectory.EnumerateDirectories();
        //    var compressedNames = artist.Compositions
        //        .Where(c => !string.IsNullOrWhiteSpace(c.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var composition = artist.Compositions.Single(x => x.CompressedName == di.Name);
        //        log.Trace($"Artist {artist.Name}, valid folder {di.Name} ({composition.Name})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Artist {artist.Name}, invalid folder {di.Name}");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateCompressedNamePerformances(Composition composition, DirectoryInfo compositionDirectory)
        //{
        //    var existingFolders = compositionDirectory.EnumerateDirectories();
        //    var compressedNames = composition.Performances
        //        .Where(p => !string.IsNullOrWhiteSpace(p.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var performance = composition.Performances.Single(x => x.CompressedName == di.Name);
        //        log.Trace($"Composition {composition.Name}, valid folder {di.Name} ({performance.GetAllPerformersCSV()})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Composer {composition.Artist.Name}, Composition {composition.Name} ({composition.CompressedName}), folder {di.Name}, performance not found in database");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        //private (IEnumerable<DirectoryInfo> validFolders, IEnumerable<DirectoryInfo> invalidFolders) ValidateFullNamePerformances(Composition composition, DirectoryInfo compositionDirectory)
        //{
        //    var existingFolders = compositionDirectory.EnumerateDirectories();
        //    var compressedNames = composition.Performances
        //        .Where(p => !string.IsNullOrWhiteSpace(p.CompressedName))
        //        .Select(x => x.CompressedName);
        //    var validFolders = existingFolders.Where(f => compressedNames.Contains(f.Name, StringComparer.InvariantCultureIgnoreCase));
        //    var invalidFolders = existingFolders.Except(validFolders, new DirectoryInfoComparer());
        //    foreach (var di in validFolders)
        //    {
        //        var performance = composition.Performances.Single(x => x.CompressedName == di.Name);
        //        log.Trace($"Composition {composition.Name}, valid folder {di.Name} ({performance.GetAllPerformersCSV()})");
        //    }
        //    foreach (var di in invalidFolders)
        //    {
        //        log.Information($"Composer {composition.Artist.Name}, Composition {composition.Name} ({composition.CompressedName}), folder {di.Name}, performance not found in database");
        //    }
        //    return (validFolders, invalidFolders);
        //}
        private string GetPerformanceFullName(Performance performance)
        {
            var name = performance.Composition.Name;
            if(performance.Composition.Performances.Count() > 1)
            {
                var index = performance.Composition.Performances.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(performance);
                name = $"{performance.Composition.Name} ({index + 1})";
            }
            return name;
        }
    }
}




