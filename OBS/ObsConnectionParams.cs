namespace ParadiseHelper.OBS
{
    /// <summary>
    /// Represents the parameters required to establish a connection with the OBS Studio WebSocket server.
    /// </summary>
    public class OBSConnectionParams
    {
        /// <summary>
        /// Gets or sets the IP address of the OBS Studio WebSocket server. Defaults to "127.0.0.1" (localhost).
        /// </summary>
        public string Ip { get; set; } = "127.0.0.1";

        /// <summary>
        /// Gets or sets the port number used by the OBS Studio WebSocket server. Defaults to 4444.
        /// </summary>
        public int Port { get; set; } = 4444;

        /// <summary>
        /// Gets or sets the authentication password required to connect to the OBS Studio WebSocket server. Defaults to an empty string.
        /// </summary>
        public string Password { get; set; } = "";
    }
}