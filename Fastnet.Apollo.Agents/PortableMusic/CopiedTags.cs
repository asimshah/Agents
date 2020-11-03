using Fastnet.Music.TagLib;

namespace Fastnet.Apollo.Agents
{
    internal class CopiedTags
    {
        #region Public Properties

        public string Album { get; set; }
        public string[] Genres { get; set; }
        public string[] Performers { get; set; }
        public Picture[] Pictures { get; set; }
        public string Title { get; set; }
        public uint Track { get; set; }

        #endregion Public Properties

        #region Public Methods

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

        #endregion Public Methods
    }
}
