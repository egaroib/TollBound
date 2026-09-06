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

    /// <summary>
    /// Stops TargetPortal refusing a crossing before Tollbound has had a say.
    ///
    /// Its click handler asks Player.IsTeleportable() first, which is vanilla's
    /// all-or-nothing check — so a Swamp portal with iron in your pack was refused with
    /// "item in inventory won't allow me to teleport" and the real gate never ran. Setting
    /// m_allowAllItems on the piece would make it skip that check, but portal mods read
    /// that flag as "no restrictions at all", which is exactly what must not happen.
    ///
    /// Instead the answer is deferred: while TargetPortal is mid-click at a biome portal,
    /// this reports teleportable and lets MapTeleportPatch make the real decision a moment
    /// later, with the destination known. Scoped to that instant and to the local player's
    /// own inventory, so nothing else sees a changed answer.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
    internal static class MapTeleportableDeferPatch
    {
        private static void Postfix(Inventory __instance, ref bool __result)
        {
            if (__result || !TargetPortalBridge.MapCrossingInProgress)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null || __instance != player.GetInventory())
            {
                return;
            }

            if (PortalIdentity.TierOf(NearbyPortal.Current) == BiomeTier.None)
            {
                return;
            }

            __result = true;
        }
    }
}
