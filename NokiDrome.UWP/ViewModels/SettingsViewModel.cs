using System.Threading.Tasks;
using NokiDrome.UWP.Models;
using NokiDrome.UWP.Services;

namespace NokiDrome.UWP.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _url      = "";
        private string _username = "";
        private string _password = "";
        private string _testResult = "";
        private bool   _isTesting  = false;

        public string Url        { get => _url;        set => Set(ref _url, value); }
        public string Username   { get => _username;   set => Set(ref _username, value); }
        public string Password   { get => _password;   set => Set(ref _password, value); }
        public string TestResult { get => _testResult; set => Set(ref _testResult, value); }
        public bool   IsTesting  { get => _isTesting;  set => Set(ref _isTesting, value); }

        public void Load()
        {
            var server = App.Settings.GetServer();
            Url      = server.Url;
            Username = server.Username;
            Password = server.Password;
        }

        public void Save()
        {
            App.Settings.SaveServer(new SubsonicServer
            {
                Url      = Url.Trim(),
                Username = Username.Trim(),
                Password = Password
            });
            App.Subsonic.Refresh();
        }

        public async Task TestConnectionAsync()
        {
            Save();
            IsTesting  = true;
            TestResult = "Testing...";
            var ok = await App.Subsonic.PingAsync();
            TestResult = ok ? "Connected" : "Connection failed — check URL and credentials";
            IsTesting  = false;
        }
    }
}
