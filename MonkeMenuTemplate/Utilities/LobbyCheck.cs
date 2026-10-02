namespace MonkeMenu.Utilities
{
    /// <summary>
    /// Gorilla Tag only allows mods in private / modded lobbies. Mods marked ModdedOnly
    /// use this to switch themselves off anywhere else, so you aren't ruining public games
    /// (and aren't risking your account).
    /// </summary>
    public static class LobbyCheck
    {
        /// <summary>True when offline, or in a room whose game mode is a modded one.</summary>
        public static bool IsSafe
        {
            get
            {
                var network = NetworkSystem.Instance;
                if (network == null || !network.InRoom) return true;

                string mode = network.GameModeString;
                return !string.IsNullOrEmpty(mode) && mode.ToUpperInvariant().Contains("MODDED");
            }
        }
    }
}
