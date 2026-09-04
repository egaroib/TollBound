using HarmonyLib;
using Tollbound.Model;

namespace Tollbound.Patches
{
    /// <summary>
    /// Teaches the game that Tollbound's pieces are portals at all.
    ///
    /// Game.Awake builds PortalPrefabHash from the serialized m_portalPrefabs list, and
    /// ZDOMan gates every portal path on it: a ZDO whose prefab hash is absent is never
    /// added to m_portalObjects, so GetPortals() never returns it and ConnectPortals()
    /// never considers it. A biome portal without this places fine, looks right, takes a
    /// tag, and then silently never pairs with anything.
    ///
    /// Only the name hash is needed, not the prefab object, so this is safe regardless of
    /// whether the pieces have been registered yet.
    /// </summary>
    [HarmonyPatch(typeof(Game), "Awake")]
    internal static class PortalRegistrationPatch
    {
        private static void Postfix(Game __instance)
        {
            var added = 0;

            foreach (var tier in Tiers.All)
            {
                var hash = tier.PortalPrefab.GetStableHashCode();

                if (__instance.PortalPrefabHash.Contains(hash))
                {
                    continue;
                }

                __instance.PortalPrefabHash.Add(hash);
                added++;
            }

            TollboundPlugin.LogInfo(
                $"Registered {added} biome portal prefab(s) as portals " +
                $"({__instance.PortalPrefabHash.Count} known to the game in total).");
        }
    }
}
