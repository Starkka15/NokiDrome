namespace NokiDrome.UWP.Models
{
    public class SubsonicServer
    {
        public string Url      { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Url) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}
