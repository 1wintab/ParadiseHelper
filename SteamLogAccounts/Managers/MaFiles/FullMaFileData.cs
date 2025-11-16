namespace ParadiseHelper.SteamLogAccounts.Managers.MaFiles
{
    /// <summary>
    /// Data structure for deserializing the core content of a Steam mobile authenticator file (.maFile).
    /// </summary>
    public class FullMaFileData
    {
        public string shared_secret { get; set; }
        public string account_name { get; set; }
        public string SteamID { get; set; }
    }
}