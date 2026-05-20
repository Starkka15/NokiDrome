using System;

namespace NokiDrome.UWP.Models
{
    public class Song
    {
        public string Id          { get; set; }
        public string Title       { get; set; }
        public string ArtistId    { get; set; }
        public string ArtistName  { get; set; }
        public string AlbumId     { get; set; }
        public string AlbumName   { get; set; }
        public int    TrackNumber { get; set; }
        public int    DiscNumber  { get; set; }
        public int    Year        { get; set; }
        public string Genre       { get; set; }
        public int    Duration    { get; set; }
        public int    BitRate     { get; set; }
        public string CoverArtId  { get; set; }
        public bool   IsStarred   { get; set; }

        public string DurationText
        {
            get
            {
                var t = TimeSpan.FromSeconds(Duration);
                if (t.TotalHours >= 1)
                    return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
                return $"{t.Minutes}:{t.Seconds:D2}";
            }
        }
    }
}
