using Windows.Security.Credentials;
using Windows.Storage;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Services
{
    // Where downloaded audio is stored.
    public enum OfflineStorage { Internal, SdCard }

    public class SettingsService
    {
        private const string ServerUrlKey      = "server_url";
        private const string UsernameKey       = "server_username";
        private const string VaultResource     = "NokiDrome";
        private const string OfflineStorageKey = "offline_storage";
        private const string AutoCacheKey      = "offline_autocache";

        private readonly ApplicationDataContainer _local =
            ApplicationData.Current.LocalSettings;

        // ── Offline settings ──────────────────────────────────────────────────

        public OfflineStorage OfflineStorageLocation
        {
            get => (_local.Values[OfflineStorageKey] as string) == "sd"
                ? OfflineStorage.SdCard
                : OfflineStorage.Internal;
            set => _local.Values[OfflineStorageKey] =
                value == OfflineStorage.SdCard ? "sd" : "internal";
        }

        // When on, tracks are cached to offline storage as they play.
        public bool AutoCache
        {
            get => _local.Values[AutoCacheKey] as bool? ?? false;
            set => _local.Values[AutoCacheKey] = value;
        }

        public SubsonicServer GetServer()
        {
            return new SubsonicServer
            {
                Url      = _local.Values[ServerUrlKey] as string ?? "",
                Username = _local.Values[UsernameKey]  as string ?? "",
                Password = LoadPassword()
            };
        }

        public void SaveServer(SubsonicServer server)
        {
            _local.Values[ServerUrlKey] = server.Url?.TrimEnd('/') ?? "";
            _local.Values[UsernameKey]  = server.Username ?? "";
            SavePassword(server.Username ?? "", server.Password ?? "");
        }

        private string LoadPassword()
        {
            try
            {
                var vault = new PasswordVault();
                var cred  = vault.Retrieve(VaultResource, _local.Values[UsernameKey] as string ?? "");
                cred.RetrievePassword();
                return cred.Password;
            }
            catch { return ""; }
        }

        private void SavePassword(string username, string password)
        {
            try
            {
                var vault = new PasswordVault();
                try { vault.Remove(vault.Retrieve(VaultResource, username)); } catch { }
                if (!string.IsNullOrEmpty(username))
                    vault.Add(new PasswordCredential(VaultResource, username, password));
            }
            catch { }
        }
    }
}
