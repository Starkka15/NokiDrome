using System.Collections.Generic;

namespace NokiDrome.UWP.Models
{
    public enum RepeatMode { Off, All, One }

    public class PlayQueue
    {
        public List<Song> Items        { get; set; } = new List<Song>();
        public int        CurrentIndex { get; set; } = -1;
        public RepeatMode RepeatMode   { get; set; } = RepeatMode.Off;
        public bool       IsShuffled   { get; set; } = false;
        public List<int>  ShuffleOrder { get; set; } = new List<int>();

        public Song CurrentSong =>
            CurrentIndex >= 0 && CurrentIndex < Items.Count
                ? Items[CurrentIndex] : null;

        public bool HasNext => CurrentIndex < Items.Count - 1 || RepeatMode != RepeatMode.Off;
        public bool HasPrev => CurrentIndex > 0;
    }
}
