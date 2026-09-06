using UnityEngine;

namespace Tollbound.Model
{
    /// <summary>
    /// Turning game objects back into Tollbound's own vocabulary: which tier a portal
    /// grants, and which prefab an inventory item actually is.
    /// </summary>
    internal static class PortalIdentity
    {
        /// <summary>
        /// The tier this portal grants. None for wood, stone, and anything else, which is
        /// the signal to leave vanilla behaviour alone.
        /// </summary>
        internal static BiomeTier TierOf(TeleportWorld portal) =>
            portal == null ? BiomeTier.None : Tiers.TierOfPortal(portal.gameObject.name);

        /// <summary>
        /// The tier of the portal on the far end of the connection, or None when there is
        /// no partner, the partner has not loaded, or the partner is a vanilla portal.
        /// </summary>
        internal static BiomeTier TierOfFarEnd(TeleportWorld portal)
        {
            var zdo = portal == null || portal.m_nview == null ? null : portal.m_nview.GetZDO();
            if (zdo == null)
            {
                return BiomeTier.None;
            }

            var connection = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (connection == ZDOID.None)
            {
                return BiomeTier.None;
            }

            var target = ZDOMan.instance == null ? null : ZDOMan.instance.GetZDO(connection);
            if (target == null || ZNetScene.instance == null)
            {
                return BiomeTier.None;
            }

            var prefab = ZNetScene.instance.GetPrefab(target.GetPrefab());
            return prefab == null ? BiomeTier.None : Tiers.TierOfPortal(prefab.name);
        }

        /// <summary>
        /// The tier of the portal nearest a world position, or None if there is none close
        /// enough. Used to recover the far end of a map-based crossing, which never goes
        /// through the connection a walked crossing uses.
        /// </summary>
        internal static BiomeTier TierOfPortalAt(Vector3 position)
        {
            if (ZDOMan.instance == null || ZNetScene.instance == null)
            {
                return BiomeTier.None;
            }

            // Generous, but far tighter than the gap between any two portals worth
            // confusing: a map crossing lands a step in front of its destination.
            const float maxDistanceSq = 25f;

            ZDO closest = null;
            var closestSq = maxDistanceSq;

            foreach (var portal in ZDOMan.instance.GetPortals())
            {
                var distanceSq = (portal.GetPosition() - position).sqrMagnitude;
                if (distanceSq >= closestSq)
                {
                    continue;
                }

                closest = portal;
                closestSq = distanceSq;
            }

            if (closest == null)
            {
                return BiomeTier.None;
            }

            var prefab = ZNetScene.instance.GetPrefab(closest.GetPrefab());
            return prefab == null ? BiomeTier.None : Tiers.TierOfPortal(prefab.name);
        }

        /// <summary>
        /// What a crossing will actually carry: the lower of the two ends.
        ///
        /// Portals of different tiers connect freely, so a Swamp portal linked to a Black
        /// Forest one is a Black Forest crossing in both directions. Linking either to a
        /// vanilla portal yields None, which carries nothing restricted at all — the same
        /// answer vanilla would give.
        /// </summary>
        internal static BiomeTier Ceiling(BiomeTier near, BiomeTier far) =>
            near < far ? near : far;

        /// <summary>
        /// The prefab name behind an inventory item, which is what config and the cargo
        /// tiers are keyed on. m_dropPrefab is set for items that came from the world;
        /// ObjectDB covers the rest by shared data identity.
        /// </summary>
        internal static string PrefabNameOf(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return null;
            }

            if (item.m_dropPrefab != null)
            {
                return item.m_dropPrefab.name;
            }

            if (item.m_shared != null && ObjectDB.instance != null
                && ObjectDB.instance.TryGetItemPrefab(item.m_shared, out var prefab)
                && prefab != null)
            {
                return prefab.name;
            }

            return null;
        }

        /// <summary>Display name for an item prefab, localized, for player-facing text.</summary>
        internal static string DisplayName(string prefabName)
        {
            if (ObjectDB.instance != null
                && ObjectDB.instance.TryGetItemPrefab(prefabName, out var go)
                && go != null)
            {
                var drop = go.GetComponent<ItemDrop>();
                if (drop != null && drop.m_itemData?.m_shared != null)
                {
                    return Localization.instance != null
                        ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name)
                        : drop.m_itemData.m_shared.m_name;
                }
            }

            return prefabName;
        }
    }
}
