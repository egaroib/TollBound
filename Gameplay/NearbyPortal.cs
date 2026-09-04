using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Gameplay
{
    /// <summary>
    /// The biome portal the local player is currently standing at, if any.
    ///
    /// TeleportWorld.UpdatePortal already runs a proximity test twice a second for the
    /// swirl, so this piggybacks on that rather than maintaining a registry of live
    /// portals or sweeping colliders. The record simply goes stale if nothing refreshes
    /// it, which is exactly what happens when the player walks away.
    /// </summary>
    internal static class NearbyPortal
    {
        /// <summary>Three UpdatePortal ticks. Long enough to survive a dropped frame.</summary>
        private const float StaleAfter = 1.5f;

        private static TeleportWorld _portal;
        private static float _distance;
        private static float _reportedAt = float.NegativeInfinity;

        internal static void Report(TeleportWorld portal, float distance)
        {
            var expired = Time.time - _reportedAt > StaleAfter;

            // Closest wins within a live window; anything wins once the record is stale.
            if (expired || _portal == null || _portal == portal || distance <= _distance)
            {
                _portal = portal;
                _distance = distance;
            }

            _reportedAt = Time.time;
        }

        /// <summary>
        /// The portal in range, or null. Unity's destroyed-object equality means a portal
        /// torn down since the last report reads as null here without any bookkeeping.
        /// </summary>
        internal static TeleportWorld Current =>
            Time.time - _reportedAt > StaleAfter || _portal == null ? null : _portal;

        /// <summary>
        /// What the crossing at the nearby portal would carry, or None when there is no
        /// portal in range. Callers use None to mean "leave vanilla behaviour alone".
        /// </summary>
        internal static BiomeTier CeilingHere()
        {
            var portal = Current;
            if (portal == null)
            {
                return BiomeTier.None;
            }

            return PortalIdentity.Ceiling(
                PortalIdentity.TierOf(portal), PortalIdentity.TierOfFarEnd(portal));
        }

        internal static bool InRange => Current != null;
    }
}
