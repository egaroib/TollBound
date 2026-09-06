using System.Collections.Generic;
using System.Linq;
using Tollbound.Gameplay.Backpacks;
using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Gameplay
{
    /// <summary>One kind of restricted item the player is carrying, and how much of it.</summary>
    internal sealed class CargoLot
    {
        internal string Prefab;
        internal BiomeTier Tier;
        internal int Count;

        /// <summary>Whether a failed loss roll may destroy this. Ores and bars only.</summary>
        internal bool Destructible;
    }

    /// <summary>
    /// What the player is carrying, sorted into biome tiers. Built once per crossing and
    /// then read several times, so the inventory is walked exactly once.
    /// </summary>
    internal sealed class Manifest
    {
        internal readonly List<CargoLot> Lots = new List<CargoLot>();

        /// <summary>
        /// Items the game marks non-teleportable that Tollbound has no tier for, usually
        /// because a game update or another mod added them. The crossing fails closed on
        /// these: letting an unrecognised ore travel free would be a silent hole, and
        /// vanilla would have refused it anyway.
        /// </summary>
        internal readonly List<string> Unrecognized = new List<string>();

        internal bool IsEmpty => Lots.Count == 0 && Unrecognized.Count == 0;

        /// <summary>Distinct tiers represented, lowest first, so tolls read in progression order.</summary>
        internal List<BiomeTier> TiersPresent =>
            Lots.Select(l => l.Tier).Distinct().OrderBy(t => (int)t).ToList();

        /// <summary>The highest tier carried, which is what the ceiling is tested against.</summary>
        internal BiomeTier HighestTier =>
            Lots.Count == 0 ? BiomeTier.None : Lots.Max(l => l.Tier);

        internal IEnumerable<CargoLot> Of(BiomeTier tier) => Lots.Where(l => l.Tier == tier);

        /// <summary>Total units of one biome's cargo, which is what a toll is priced against.</summary>
        internal int UnitsOf(BiomeTier tier) => Of(tier).Sum(l => l.Count);

        /// <summary>Cached for the readers that run every frame. See Cached.</summary>
        private static Manifest _cached;
        private static float _cachedAt = float.NegativeInfinity;

        /// <summary>
        /// A manifest good enough for display. Scanning is cheap for the player's own
        /// inventory but not for a backpack mod that can only be queried one item name at a
        /// time, and the hover text asks every frame. The gate always builds fresh.
        /// </summary>
        internal static Manifest Cached(Player player)
        {
            if (_cached != null && Time.time - _cachedAt < 0.25f)
            {
                return _cached;
            }

            _cached = Build(player);
            _cachedAt = Time.time;
            return _cached;
        }

        internal static Manifest Build(Player player)
        {
            var manifest = new Manifest();
            var inventory = player == null ? null : player.GetInventory();
            if (inventory == null)
            {
                return manifest;
            }

            var byPrefab = new Dictionary<string, CargoLot>();

            foreach (var item in inventory.GetAllItems())
            {
                var prefab = PortalIdentity.PrefabNameOf(item);
                if (prefab == null)
                {
                    continue;
                }

                var tier = CargoRegistry.TierOfItem(prefab);
                if (tier == BiomeTier.None)
                {
                    var restrictedByGame = item.m_shared != null && !item.m_shared.m_teleportable;
                    if (restrictedByGame && !manifest.Unrecognized.Contains(prefab))
                    {
                        manifest.Unrecognized.Add(prefab);
                    }

                    continue;
                }

                if (!byPrefab.TryGetValue(prefab, out var lot))
                {
                    lot = new CargoLot
                    {
                        Prefab = prefab,
                        Tier = tier,
                        Destructible = CargoRegistry.CanBeLost(prefab),
                    };
                    byPrefab[prefab] = lot;
                    manifest.Lots.Add(lot);
                }

                lot.Count += item.m_stack;
            }

            // Ore in a pack is ore. Without this the whole ruleset is bypassed by moving
            // items one slot to the left.
            BackpackBridge.CollectCargo(player, (prefab, count) => Record(manifest, byPrefab, prefab, count));

            return manifest;
        }

        /// <summary>Folds one stack of cargo into the manifest, wherever it was found.</summary>
        private static void Record(
            Manifest manifest, Dictionary<string, CargoLot> byPrefab, string prefab, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var tier = CargoRegistry.TierOfItem(prefab);
            if (tier == BiomeTier.None)
            {
                if (!manifest.Unrecognized.Contains(prefab) && IsRestrictedByGame(prefab))
                {
                    manifest.Unrecognized.Add(prefab);
                }

                return;
            }

            if (!byPrefab.TryGetValue(prefab, out var lot))
            {
                lot = new CargoLot
                {
                    Prefab = prefab,
                    Tier = tier,
                    Destructible = CargoRegistry.CanBeLost(prefab),
                };
                byPrefab[prefab] = lot;
                manifest.Lots.Add(lot);
            }

            lot.Count += count;
        }

        private static bool IsRestrictedByGame(string prefab)
        {
            if (ObjectDB.instance == null
                || !ObjectDB.instance.TryGetItemPrefab(prefab, out var go)
                || go == null)
            {
                return false;
            }

            var drop = go.GetComponent<ItemDrop>();
            return drop?.m_itemData?.m_shared != null && !drop.m_itemData.m_shared.m_teleportable;
        }
    }

    /// <summary>Counting and removing by prefab name, which vanilla's Inventory does not offer.</summary>
    internal static class Cargo
    {
        internal static int Count(Player player, string prefabName)
        {
            var inventory = player == null ? null : player.GetInventory();
            if (inventory == null || prefabName == null)
            {
                return 0;
            }

            var carried = inventory.GetAllItems()
                .Where(i => PortalIdentity.PrefabNameOf(i) == prefabName)
                .Sum(i => i.m_stack);

            return carried + BackpackBridge.Count(player, prefabName);
        }

        /// <summary>
        /// Removes up to <paramref name="amount"/> units and returns how many were actually
        /// taken. Iterates a copy because RemoveItem mutates the inventory's own list.
        /// </summary>
        internal static int Remove(Player player, string prefabName, int amount)
        {
            var inventory = player == null ? null : player.GetInventory();
            if (inventory == null || prefabName == null || amount <= 0)
            {
                return 0;
            }

            var taken = 0;

            foreach (var item in inventory.GetAllItems().ToList())
            {
                if (taken >= amount)
                {
                    break;
                }

                if (PortalIdentity.PrefabNameOf(item) != prefabName)
                {
                    continue;
                }

                var take = System.Math.Min(item.m_stack, amount - taken);
                inventory.RemoveItem(item, take);
                taken += take;
            }

            // Hands first, then packs. A player holding entrails should spend those before
            // the mod goes rummaging, and losses must be able to reach a pack or stashing
            // ore in one would make it immune.
            if (taken < amount)
            {
                taken += BackpackBridge.Remove(player, prefabName, amount - taken);
            }

            return taken;
        }
    }
}
