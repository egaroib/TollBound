using System.Collections.Generic;
using HarmonyLib;
using Tollbound.Model;

namespace Tollbound.Patches
{
    /// <summary>
    /// Portals pair on tag alone. Game.FindRandomUnconnectedPortal filters candidates by
    /// ZDOVars.s_tag and nothing else, so without this a Swamp portal tagged "base" would
    /// happily connect to a wood portal tagged "base" and carry iron through it.
    ///
    /// Runs host-side: ConnectPortals is driven by the server (or the peer-host), which is
    /// why the mod has to be installed there and not only on clients.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FindRandomUnconnectedPortal))]
    internal static class PortalPairingPatch
    {
        /// <summary>
        /// Postfix rather than a replacement prefix, so the original still runs and other
        /// mods patching the same method still see it. It only intervenes when vanilla's
        /// pick crosses tiers, and then re-picks with the prefab constraint applied so the
        /// connection still lands on the first cycle instead of waiting for a lucky roll.
        /// </summary>
        private static void Postfix(
            Game __instance, List<ZDO> portals, ZDO skip, string tag, ref ZDO __result)
        {
            if (__result == null || skip == null)
            {
                return;
            }

            var wanted = skip.GetPrefab();
            if (__result.GetPrefab() == wanted)
            {
                return;
            }

            // Vanilla lets wood and stone portals connect to each other. That stays true:
            // only intervene when a Tollbound portal is on one end or the other.
            if (!InvolvesTollboundPortal(skip, __result))
            {
                return;
            }

            var match = FindSameTier(__instance, portals, skip, tag, wanted);

            TollboundPlugin.LogVerbose(
                match != null
                    ? $"Pairing: re-picked a same-tier portal for tag '{tag}'."
                    : $"Pairing: no same-tier portal available for tag '{tag}', leaving unconnected.");

            __result = match;
        }

        private static bool InvolvesTollboundPortal(ZDO a, ZDO b) =>
            IsTollbound(a) || IsTollbound(b);

        private static bool IsTollbound(ZDO zdo)
        {
            var prefab = ZNetScene.instance != null
                ? ZNetScene.instance.GetPrefab(zdo.GetPrefab())
                : null;

            return prefab != null && Tiers.IsTollboundPortal(prefab.name);
        }

        /// <summary>
        /// Vanilla's candidate filter with one extra clause: the prefab must match.
        /// Mirrors Game.FindRandomUnconnectedPortal so behaviour stays identical apart
        /// from the tier restriction.
        /// </summary>
        private static ZDO FindSameTier(
            Game game, List<ZDO> portals, ZDO skip, string tag, int wantedPrefab)
        {
            var candidates = new List<ZDO>();

            foreach (var portal in portals)
            {
                if (portal == skip
                    || portal.GetPrefab() != wantedPrefab
                    || portal.GetString(ZDOVars.s_tag) != tag
                    || portal.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None
                    || game.IsCurrentlyConnectingPortal(portal))
                {
                    continue;
                }

                candidates.Add(portal);
            }

            return candidates.Count == 0
                ? null
                : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }
}
