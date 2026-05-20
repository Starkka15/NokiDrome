using System.Collections.Generic;

namespace NokiDrome.UWP.Models
{
    public class Playlist
    {
        public string      Id        { get; set; }
        public string      Name      { get; set; }
        public int         SongCount { get; set; }
        public int         Duration  { get; set; }
        public string      CoverArtId { get; set; }
        public List<Song>  Entries   { get; set; } = new List<Song>();
    }
}
