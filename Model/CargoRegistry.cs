using System;
using System.Collections.Generic;
using System.Linq;

namespace Tollbound.Model
{
    /// <summary>
    /// Which biome each restricted item answers to, and whether it can be destroyed in
    /// transit. Built once per game start from the tier table plus any config overrides,
    /// then read on every teleport, so lookups are dictionaries rather than scans.
    /// </summary>
    internal static class CargoRegistry
    {
        private static readonly Dictionary<string, BiomeTier> TierOf =
            new Dictionary<string, BiomeTier>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> Destructible =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsBuilt { get; private set; }

        /// <summary>
        /// Every restricted prefab, for backpack mods that can only be asked about items by
        /// name rather than enumerated.
        /// </summary>
        internal static IEnumerable<string> AllCargo => TierOf.Keys;

        internal static void Build()
        {
            TierOf.Clear();
            Destructible.Clear();

            foreach (var tier in Tiers.All)
            {
                foreach (var item in tier.Cargo)
                {
                    TierOf[item] = tier.Tier;
                }

                foreach (var item in tier.LossEligible)
                {
                    Destructible.Add(item);
                }
            }

            IsBuilt = true;
            WarnAboutUnclassifiedItems();

            TollboundPlugin.LogInfo(
                $"Cargo registry: {TierOf.Count} restricted items across " +
                $"{Tiers.All.Length} tiers, {Destructible.Count} of them loss-eligible.");
        }

        /// <summary>
        /// The tier an item belongs to, or None if Tollbound does not restrict it.
        /// </summary>
        internal static BiomeTier TierOfItem(string prefabName) =>
            prefabName != null && TierOf.TryGetValue(prefabName, out var tier)
                ? tier
                : BiomeTier.None;

        /// <summary>
        /// Whether this item can be destroyed by a failed loss roll. Quest and mechanism
        /// items return false: they are gated by tier and always arrive intact.
        /// </summary>
        internal static bool CanBeLost(string prefabName) =>
            prefabName != null && Destructible.Contains(prefabName);

        /// <summary>
        /// Reports any item the game marks non-teleportable that Tollbound has no tier for.
        /// Without this an item added by a game update or another mod would silently pass
        /// through every biome portal for free, which is exactly the failure that looks
        /// like the mod working.
        /// </summary>
        private static void WarnAboutUnclassifiedItems()
        {
            if (ObjectDB.instance == null)
            {
                return;
            }

            var unclassified = new List<string>();

            foreach (var go in ObjectDB.instance.m_items)
            {
                if (go == null)
                {
                    continue;
                }

                var drop = go.GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared == null || drop.m_itemData.m_shared.m_teleportable)
                {
                    continue;
                }

                if (!TierOf.ContainsKey(go.name))
                {
                    unclassified.Add(go.name);
                }
            }

            if (unclassified.Count == 0)
            {
                return;
            }

            TollboundPlugin.LogWarning(
                $"{unclassified.Count} non-teleportable item(s) have no Tollbound tier and " +
                "will pass through biome portals untolled: " +
                string.Join(", ", unclassified.OrderBy(n => n).ToArray()));
        }
    }
}
