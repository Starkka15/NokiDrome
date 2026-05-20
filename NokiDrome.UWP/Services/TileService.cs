using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Services
{
    public static class TileService
    {
        public static void Update(Song song)
        {
            if (song == null) return;

            string artUrl = Escape(string.IsNullOrEmpty(song.CoverArtId)
                ? "ms-appx:///Assets/Square150x150Logo.png"
                : App.Subsonic.GetCoverArtUrl(song.CoverArtId, 150));

            string title  = Escape(song.Title);
            string artist = Escape(song.ArtistName);
            string album  = Escape(song.AlbumName);

            string xml = $@"<tile>
  <visual branding=""nameAndLogo"">
    <binding template=""TileMedium"">
      <image src=""{artUrl}"" placement=""background"" hint-overlay=""55""/>
      <text hint-style=""body"">{title}</text>
      <text hint-style=""captionSubtle"">{artist}</text>
    </binding>
    <binding template=""TileWide"">
      <group>
        <subgroup hint-weight=""1"">
          <image src=""{artUrl}"" hint-crop=""none""/>
        </subgroup>
        <subgroup hint-weight=""2"" hint-textStacking=""center"">
          <text hint-style=""body"">{title}</text>
          <text hint-style=""captionSubtle"">{artist}</text>
          <text hint-style=""captionSubtle"">{album}</text>
        </subgroup>
      </group>
    </binding>
    <binding template=""TileLarge"">
      <image src=""{artUrl}"" placement=""background"" hint-overlay=""40""/>
      <text hint-style=""subtitle"">{title}</text>
      <text hint-style=""body"">{artist}</text>
      <text hint-style=""captionSubtle"">{album}</text>
    </binding>
  </visual>
</tile>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            TileUpdateManager.CreateTileUpdaterForApplication()
                             .Update(new TileNotification(doc));
        }

        public static void Clear()
            => TileUpdateManager.CreateTileUpdaterForApplication().Clear();

        private static string Escape(string s)
            => s == null ? "" : s.Replace("&", "&amp;")
                                 .Replace("<", "&lt;")
                                 .Replace(">", "&gt;")
                                 .Replace("\"", "&quot;");
    }
}
