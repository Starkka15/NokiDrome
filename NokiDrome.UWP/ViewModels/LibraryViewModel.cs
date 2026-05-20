using System.Collections.Generic;
using System.Threading.Tasks;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.ViewModels
{
    public class LibraryViewModel
    {
        public Task<List<Song>>     GetStarredSongsAsync() => App.Subsonic.GetStarredSongsAsync();
        public Task<List<Album>>    GetAlbumsAsync()    => App.Subsonic.GetAlbumListAsync("alphabeticalByName", 100);
        public Task<List<Artist>>   GetArtistsAsync()   => App.Subsonic.GetArtistsAsync();
        public Task<List<Song>>     GetRandomSongsAsync() => App.Subsonic.GetRandomSongsAsync(50);
        public Task<List<Genre>>    GetGenresAsync()    => App.Subsonic.GetGenresAsync();
        public Task<List<Playlist>> GetPlaylistsAsync() => App.Subsonic.GetPlaylistsAsync();

        public void PlaySong(Song song, IList<Song> context)
        {
            var list = new List<Song>(context);
            int idx  = list.IndexOf(song);
            App.Player.SetQueue(list, idx < 0 ? 0 : idx);
            App.Navigation.NavigateToNowPlaying();
        }

        public void ShuffleAll(IList<Song> songs)
        {
            App.Player.ShuffleAndPlay(new List<Song>(songs));
            App.Navigation.NavigateToNowPlaying();
        }
    }
}
