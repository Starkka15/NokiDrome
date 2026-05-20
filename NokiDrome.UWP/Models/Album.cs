namespace NokiDrome.UWP.Models
{
    public class Album
    {
        public string Id         { get; set; }
        public string Name       { get; set; }
        public string ArtistId   { get; set; }
        public string ArtistName { get; set; }
        public int    Year       { get; set; }
        public string Genre      { get; set; }
        public int    SongCount  { get; set; }
        public int    Duration   { get; set; }
        public string CoverArtId { get; set; }
    }
}
