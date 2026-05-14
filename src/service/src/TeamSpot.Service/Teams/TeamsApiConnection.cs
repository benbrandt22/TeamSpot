using System.Reflection;

namespace TeamSpot.Service.Teams
{
    public class TeamsApiConnection
    {
        private Assembly _assembly;

        public TeamsApiConnection(string? token)
        {
            Token = token;
            _assembly = Assembly.GetExecutingAssembly();
        }

        public string? Token { get; set; } = null;
        public string Manufacturer { get; } = "B2";
        public string Device { get; } = "TeamSpot";
        public string App => _assembly.GetName().Name!;
        public string AppVersion => _assembly.GetName().Version!.ToString();


        public Uri ToTeamsApiUrl()
        {
            var queryParams = new Dictionary<string, string?> {
                { "protocol-version", "2.0.0" },
                { "manufacturer", Manufacturer },
                { "device", Device },
                { "app", App },
                { "app-version", AppVersion },
                { "token", Token },
            };

            var query = string.Join("&", queryParams
                .Where(kv => !string.IsNullOrEmpty(kv.Value)) // ignores null or empty values (e.g. token if not set)
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

            var teamsApiUrl = $"ws://localhost:8124?{query}";

            return new Uri(teamsApiUrl);
        }
    }
}
