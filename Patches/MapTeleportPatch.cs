using HarmonyLib;
using Tollbound.Gameplay;
using Tollbound.Gameplay.Portals;
using Tollbound.Messages;
using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Patches
{
    /// <summary>
    /// The gate for map-based portal travel.
    ///
    /// Sits on vanilla's own TeleportTo rather than inside any particular portal mod, and
    /// only acts while TargetPortal is mid-click. Everything else that teleports a player —
    /// respawns, console commands, Tollbound's own crossings — passes straight through.
    ///
    /// The destination is recovered from the position being teleported to: a map portal
    /// puts you a step in front of the portal you picked, so the nearest portal ZDO to that
    /// point is the far end. That keeps the same min-of-both-ends ceiling as walking through.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    internal static class MapTeleportPatch
    {
        private static bool Prefix(
            Player __instance, Vector3 pos, bool distantTeleport, ref bool __result)
        {
            if (!TargetPortalBridge.MapCrossingInProgress || !distantTeleport)
            {
                return true;
            }

            // Which portal the player is standing in, tracked for the swirl already.
            var origin = NearbyPortal.Current;
            var near = PortalIdentity.TierOf(origin);

            // Leaving a wood or stone portal is TargetPortal's business, not ours.
            if (near == BiomeTier.None)
            {
                return true;
            }

            var far = PortalIdentity.TierOfPortalAt(pos);
            var verdict = TollGate.Evaluate(__instance, near, far);

            if (!verdict.Allowed)
            {
                TollboundPlugin.LogVerbose(
                    $"Refused a map crossing {near} -> {far} (ceiling {verdict.Ceiling}): " +
                    $"{verdict.Refusal}.");

                Voice.Refused(__instance, verdict);
                __result = false;
                return false;
            }

            var losses = TollGate.Apply(__instance, verdict);
            Voice.Crossed(__instance, verdict, losses);

            TollboundPlugin.LogVerbose(
                $"Allowed a map crossing {near} -> {far} (ceiling {verdict.Ceiling}), " +
                $"{verdict.Tolls.Count} toll(s), {losses.Count} loss group(s).");

            return true;
        }
    }
}
