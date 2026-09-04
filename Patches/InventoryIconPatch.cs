using HarmonyLib;
using Tollbound.Gameplay;
using Tollbound.Model;

namespace Tollbound.Patches
{
    /// <summary>
    /// Makes the no-teleport slash tell the truth at a biome portal.
    ///
    /// Vanilla stamps it from m_shared.m_teleportable alone, which knows nothing about
    /// tiers, so at a Swamp portal your iron would still look forbidden. Standing at a
    /// portal, the slash should clear from cargo this crossing accepts and stay on
    /// everything above its ceiling.
    ///
    /// A postfix rather than a transpiler: UpdateGui maps items to slots through
    /// GetElement(item.m_gridPos...), so the same mapping can simply be walked again and
    /// the flag overwritten. Away from a portal nothing is touched and vanilla stands.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryIconPatch
    {
        private static void Postfix(InventoryGrid __instance)
        {
            if (!NearbyPortal.InRange || __instance == null || __instance.m_inventory == null)
            {
                return;
            }

            // Only the player's own grid. A chest's contents are not going anywhere.
            var player = Player.m_localPlayer;
            if (player == null || __instance.m_inventory != player.GetInventory())
            {
                return;
            }

            var ceiling = NearbyPortal.CeilingHere();
            var width = __instance.m_inventory.GetWidth();

            foreach (var item in __instance.m_inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_shared.m_teleportable)
                {
                    continue;
                }

                var element = ElementFor(__instance, item, width);
                if (element?.m_noteleport == null)
                {
                    continue;
                }

                element.m_noteleport.enabled = IsBlocked(item, ceiling);
            }
        }

        /// <summary>
        /// Blocked when the item sits above what this crossing carries, or when Tollbound
        /// has no tier for it at all — the gate fails closed on those, so the icon must
        /// agree rather than promising a crossing that will be refused.
        /// </summary>
        private static bool IsBlocked(ItemDrop.ItemData item, BiomeTier ceiling)
        {
            var prefab = PortalIdentity.PrefabNameOf(item);
            var tier = CargoRegistry.TierOfItem(prefab);

            return tier == BiomeTier.None || tier > ceiling;
        }

        /// <summary>
        /// Mirrors UpdateGui's own item-to-slot lookup. Guarded because a grid mid-resize
        /// can briefly hold fewer elements than the inventory has slots.
        /// </summary>
        private static InventoryGrid.Element ElementFor(
            InventoryGrid grid, ItemDrop.ItemData item, int width)
        {
            var index = item.m_gridPos.y * width + item.m_gridPos.x;

            return index >= 0 && index < grid.m_elements.Count ? grid.m_elements[index] : null;
        }
    }
}
