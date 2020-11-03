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
    public abstract class PortWesternClassicalMusic<T> : PortMusic<T> where T : BaseFolderMethods
    {
        #region Protected Constructors

        protected PortWesternClassicalMusic(T folderNamer, MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(folderNamer, musicOptions, MusicStyles.WesternClassical, configuration, environment, portConfig)
        {
        }

        #endregion Protected Constructors

        #region Internal Methods

        internal override CopiedTags LoadTags(Track track)
        {
            var ct = new CopiedTags
            {
                Performers = new string[] { track.Performance.Composition.Artist.Name },
                Album = track.Performance.Composition.Name,
                Track = (uint)track.MovementNumber,// track.Number,
                Title = track.Title,
                Genres = new string[] { this.musicStyle.ToDescription() }
            };
            SetPicture(ct, track);
            var perfomanceCount = track.Performance.Composition.Performances.Count();
            if (perfomanceCount > 1)
            {
                var index = track.Performance.Composition.Performances.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(track.Performance);
                ct.Album = $"{track.Performance.Composition.Name} ({index + 1})";
            }
            return ct;
        }

        #endregion Internal Methods

        #region Protected Methods

        protected override Task StartAsync(IEnumerable<ArtistSet> artistSets)
        {
            Debug.Assert(artistSets.All(x => x.Artists.Count() == 1));

            foreach (var artist in artistSets.SelectMany(x => x.Artists).OrderBy(x => x.Name))
            {
                var artistDirectory = folderNamer.GetArtistDirectory(artist);
                ProcessArtist(artist, artistDirectory);
            }
            return Task.CompletedTask;
        }

        #endregion Protected Methods

        #region Private Methods

        private string GetPerformanceFullName(Performance performance)
        {
            var name = performance.Composition.Name;
            if (performance.Composition.Performances.Count() > 1)
            {
                var index = performance.Composition.Performances.OrderBy(x => x.GetAllPerformersCSV()).ToList().IndexOf(performance);
                name = $"{performance.Composition.Name} ({index + 1})";
            }
            return name;
        }
        private void ProcessArtist(Artist artist, DirectoryInfo artistDirectory)
        {
            var performanceTuples = artist.Compositions.SelectMany(x => x.Performances)
                .Select(x => new PerformanceTuple { Performance = x, Name = GetPerformanceFullName(x) })
                .OrderBy(x => x.Name);
            var allPerformances = performanceTuples.Select(x => x.Performance);
            (_, var invalidFullNameFolders) = folderNamer.ValidatePerformanceFolders(artistDirectory, performanceTuples);// ValidateFullNamePerformanceFolders(/*artist,*/ fullNamePath, performanceTuples);
            folderNamer.RemoveInvalidFolders(invalidFullNameFolders);
            foreach (var tuple in performanceTuples)
            {
                PortPerformance(artistDirectory, tuple, allPerformances);
            }
        }

        #endregion Private Methods
    }

    public class PortWesternClassicalMusicCN : PortWesternClassicalMusic<WCMCompressedNames>
    {
        #region Public Constructors

        public PortWesternClassicalMusicCN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment, PortabilityConfiguration portConfig)
            : base(new WCMCompressedNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors
    }

    public class PortWesternClassicalMusicFN : PortWesternClassicalMusic<WCMFullNames>
    {
        #region Public Constructors

        public PortWesternClassicalMusicFN(MusicOptions musicOptions, IConfiguration configuration, IWebHostEnvironment environment,
            PortabilityConfiguration portConfig) : base(new WCMFullNames(portConfig), musicOptions, configuration, environment, portConfig)
        {
        }

        #endregion Public Constructors

    }

    public class WCMCompressedNames : BaseCompressedNameMethods
    {
        #region Public Constructors

        public WCMCompressedNames(PortabilityConfiguration portConfig) : base(MusicStyles.WesternClassical, portConfig)
        {
        }

        #endregion Public Constructors
        #region Private Methods

        #endregion Private Methods
    }

    public class WCMFullNames : BaseFullNameMethods
    {
        #region Public Constructors

        public WCMFullNames(PortabilityConfiguration portConfig) : base(MusicStyles.WesternClassical, portConfig)
        {
        }

        #endregion Public Constructors
        #region Public Methods

        #endregion Public Methods
    }
}




