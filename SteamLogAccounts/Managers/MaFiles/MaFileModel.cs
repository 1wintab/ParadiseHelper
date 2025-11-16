using Newtonsoft.Json;

namespace ParadiseHelper.Managers.MaFiles
{
    public class SessionData
    {
        [JsonProperty("SteamID")]
        public ulong SteamID { get; set; }
    }

    /// <summary>
    /// Flexible model for deserializing .maFile data.
    /// Designed to read both full and truncated maFiles.
    /// </summary>
    public class MaFileModel
    {
        [JsonProperty("shared_secret")]
        public string shared_secret { get; set; }

        [JsonProperty("account_name")]
        public string account_name { get; set; }

        [JsonProperty("Session")]
        public SessionData Session { get; set; }
    }
}