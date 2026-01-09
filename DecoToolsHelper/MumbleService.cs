using Gw2Sharp;
using Gw2Sharp.Mumble;

namespace DecoToolsHelper
{
    /// Provides access to GW2 MumbleLink data.
    /// 
    /// This service:
    /// - Reads GW2 shared memory directly
    /// - Requires NO API key
    /// - Updates in real time while the game is running
    /// 
    /// Used for:
    /// - Player position
    /// - Current map ID
    public static class MumbleService
    {
        // Gw2Sharp Mumble client bound to the default "MumbleLink" shared memory block
        private static readonly IGw2MumbleClient _client =
            new Gw2Client().Mumble["MumbleLink"];

        /// <summary>
        /// Attempts to read the current map ID and player position from MumbleLink.
        /// </summary>
        /// <param name="mapId">Current GW2 map ID</param>
        /// <param name="x">Player X position (Mumble coordinate space)</param>
        /// <param name="y">Player Y position (Mumble coordinate space)</param>
        /// <param name="z">Player Z position (Mumble coordinate space)</param>
        /// <returns>
        /// True if valid Mumble data is available.
        /// False if the game is not running or not yet fully in-world.
        /// </returns>
        public static bool TryGet(out int mapId, out float x, out float y, out float z)
        {
            mapId = 0;
            x = y = z = 0;

            // Refresh the shared memory snapshot
            _client.Update();

            // MumbleLink exists but GW2 has not populated data yet
            if (!_client.IsAvailable)
                return false;

            // MapId == 0 indicates the character is not fully loaded into a map
            // (e.g., character select, loading screen)
            if (_client.MapId == 0)
                return false;

            mapId = _client.MapId;

            // AvatarPosition is provided in Mumble coordinate space
            x = (float)_client.AvatarPosition.X;
            y = (float)_client.AvatarPosition.Y;
            z = (float)_client.AvatarPosition.Z;

            return true;
        }
    }
}
