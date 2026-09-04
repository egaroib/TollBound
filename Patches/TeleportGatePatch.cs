using HarmonyLib;
using Tollbound.Gameplay;
using Tollbound.Messages;
using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Patches
{
    /// <summary>
    /// The gate itself. Runs on the travelling player's own client, which is the only place
    /// their inventory is authoritative.
    ///
    /// This owns the crossing rather than letting vanilla's check run, because vanilla's
    /// check is all-or-nothing and Tollbound's is not. It deliberately does NOT set
    /// m_allowAllItems on the piece: other portal mods read that flag as "this portal has
    /// no restrictions" and would skip their own safeguards, which would make Tollbound
    /// portals more permissive than vanilla ones rather than less.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    internal static class TeleportGatePatch
    {
        private static bool Prefix(TeleportWorld __instance, Player player)
        {
            var near = PortalIdentity.TierOf(__instance);

            // Wood and stone portals fall straight through to vanilla, untouched.
            if (near == BiomeTier.None || player == null)
            {
                return true;
            }

            if (!__instance.TargetFound())
            {
                return false;
            }

            // Mirrors vanilla's world-level guards, which sit ahead of the item check.
            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_blocked");
                return false;
            }

            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals)
                && (RandEventSystem.instance.GetBossEvent() != null
                    || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float active)
                        && active > 0f)))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
                return false;
            }

            var far = PortalIdentity.TierOfFarEnd(__instance);
            var verdict = TollGate.Evaluate(player, near, far);

            if (!verdict.Allowed)
            {
                TollboundPlugin.LogVerbose(
                    $"Refused a {near} -> {far} crossing (ceiling {verdict.Ceiling}): {verdict.Refusal}.");
                Voice.Refused(player, verdict);
                return false;
            }

            // Nothing is spent until every check has passed.
            var losses = TollGate.Apply(player, verdict);
            Voice.Crossed(player, verdict, losses);

            TollboundPlugin.LogVerbose(
                $"Allowed a {near} -> {far} crossing (ceiling {verdict.Ceiling}), " +
                $"{verdict.Tolls.Count} toll(s), {losses.Count} loss group(s).");

            Cross(__instance, player);
            return false;
        }

        /// <summary>Mirrors the movement half of TeleportWorld.Teleport.</summary>
        private static void Cross(TeleportWorld portal, Player player)
        {
            var connection = portal.m_nview.GetZDO()
                .GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);

            var target = ZDOMan.instance.GetZDO(connection);
            if (target == null)
            {
                return;
            }

            var rotation = target.GetRotation();
            var destination = target.GetPosition()
                              + rotation * Vector3.forward * portal.m_exitDistance
                              + Vector3.up;

            player.TeleportTo(destination, rotation, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
        }
    }

    /// <summary>
    /// Keeps the proximity swirl honest. Vanilla lights it from IsTeleportable(), which is
    /// all-or-nothing, so at a biome portal it would stay dark whenever you carried any ore
    /// at all — telling you the crossing is impossible when Tollbound would allow it.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
    internal static class PortalSwirlPatch
    {
        private static void Postfix(TeleportWorld __instance)
        {
            var near = PortalIdentity.TierOf(__instance);
            if (near == BiomeTier.None
                || __instance.m_proximityRoot == null
                || __instance.m_target_found == null
                || __instance.m_nview == null
                || !__instance.m_nview.IsValid())
            {
                return;
            }

            // The local player's, because the swirl answers "can I go", and because a
            // remote player's inventory is not readable here anyway.
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var distance = Vector3.Distance(
                player.transform.position, __instance.m_proximityRoot.position);

            if (distance > __instance.m_activationRange)
            {
                return;
            }

            var far = PortalIdentity.TierOfFarEnd(__instance);
            var verdict = TollGate.Evaluate(player, near, far);

            __instance.m_target_found.SetActive(verdict.Allowed && __instance.TargetFound());
        }
    }
}
