using System.Collections.Generic;
using BepInEx.Configuration;
using Tollbound.Model;

namespace Tollbound.Config
{
    /// <summary>How a toll grows with the size of the haul.</summary>
    internal enum TollScaling
    {
        /// <summary>One toll per biome per crossing, whatever you are carrying.</summary>
        Flat,

        /// <summary>
        /// One toll per load. A load defaults to 30 units, which is Valheim's own ore
        /// stack size, so the rule reads as "a toll per stack" rather than as arithmetic.
        /// </summary>
        PerLoad,
    }

    /// <summary>
    /// Every value the mod can be tuned on. Anything that changes a game rule is bound
    /// admin-only, so a server dictates it and a client cannot quietly zero out its own
    /// loss rate. Appearance is deliberately not admin-only: it changes nothing but what
    /// the player sees on their own screen.
    /// </summary>
    internal static class TollboundConfig
    {
        internal static ConfigEntry<bool> VerboseLogging;
        internal static ConfigEntry<bool> WriteItemReport;

        internal static ConfigEntry<TollScaling> Scaling;

        internal static ConfigEntry<bool> SpiritDialogue;

        internal static ConfigEntry<float> IdleGlowIntensity;
        internal static ConfigEntry<float> ConnectedGlowIntensity;

        private sealed class TierEntries
        {
            internal ConfigEntry<string> TollItem;
            internal ConfigEntry<int> TollAmount;
            internal ConfigEntry<float> LossRate;
            internal ConfigEntry<int> LoadSize;
        }

        private static readonly Dictionary<BiomeTier, TierEntries> PerTier =
            new Dictionary<BiomeTier, TierEntries>();

        /// <summary>
        /// Toll item per tier after checking it actually exists. See ResolveTollItems.
        /// </summary>
        private static readonly Dictionary<BiomeTier, string> ResolvedTollItem =
            new Dictionary<BiomeTier, string>();

        internal static void Bind(ConfigFile cfg)
        {
            VerboseLogging = cfg.Bind(
                "General", "VerboseLogging", false,
                "Log detailed per-event output. Noisy; for debugging only.");

            WriteItemReport = cfg.Bind(
                "Diagnostics", "WriteItemReport", true,
                "Write BepInEx/config/Tollbound/item-report.md on game start, listing every " +
                "non-teleportable item in this install. Used to tier cargo against what the " +
                "game actually contains rather than assumed prefab names.");

            Scaling = cfg.Bind(
                "Tolls", "Scaling", TollScaling.Flat,
                new ConfigDescription(
                    "Flat charges one toll per biome per crossing, whatever the size of the " +
                    "haul. PerLoad charges one toll for every LoadSize units of that " +
                    "biome's cargo, so moving a warehouse costs proportionally more and " +
                    "boats stay worth using for bulk.",
                    null, AdminOnly()));

            SpiritDialogue = cfg.Bind(
                "Appearance", "SpiritDialogue", true,
                "Show the spirits' centre-screen lines when a crossing is refused or paid " +
                "for. Turn off to keep only the itemised top-left ledger. Lines live in " +
                "BepInEx/config/Tollbound/voice.txt and can be edited or added to without " +
                "reinstalling. Local only.");

            IdleGlowIntensity = cfg.Bind(
                "Appearance", "IdleGlowIntensity", 2f,
                new ConfigDescription(
                    "Brightness of a biome portal's glow while it has no partner. Vanilla " +
                    "portals sit at 0 (unlit) until they pair; a low value here keeps each " +
                    "portal identifiable by colour across a base. Values above 1 bloom. Visual only.",
                    new AcceptableValueRange<float>(0f, 5f)));

            ConnectedGlowIntensity = cfg.Bind(
                "Appearance", "ConnectedGlowIntensity", 5f,
                new ConfigDescription(
                    "Brightness of a biome portal's glow once connected. Vanilla uses 5. " +
                    "Visual only.",
                    new AcceptableValueRange<float>(0.5f, 12f)));

            foreach (var tier in Tiers.All)
            {
                var section = $"Tier - {tier.Tier}";

                PerTier[tier.Tier] = new TierEntries
                {
                    TollItem = cfg.Bind(section, "TollItem", tier.DefaultTollItem,
                        new ConfigDescription(
                            $"Prefab name of the item {tier.SpiritName} takes as passage for " +
                            "this biome's cargo. Charged once per crossing, per biome carried.",
                            null, AdminOnly())),

                    TollAmount = cfg.Bind(section, "TollAmount", tier.DefaultTollAmount,
                        new ConfigDescription(
                            "How many of the toll item each crossing costs. Set to 0 to make " +
                            "this biome's cargo toll-free.",
                            new AcceptableValueRange<int>(0, 100), AdminOnly())),

                    LoadSize = cfg.Bind(section, "LoadSize", 30,
                        new ConfigDescription(
                            "Units of this biome's cargo covered by a single toll when " +
                            "Scaling is PerLoad. Ignored under Flat. Defaults to one ore " +
                            "stack, so a second stack costs a second toll.",
                            new AcceptableValueRange<int>(1, 999), AdminOnly())),

                    LossRate = cfg.Bind(section, "LossRate", tier.DefaultLossRate,
                        new ConfigDescription(
                            $"Chance, rolled once per unit, that a piece of this biome's ore " +
                            $"or bars is destroyed in transit while {tier.SpiritName} still " +
                            "lives. Never applies to quest or mechanism items. 0 disables it.",
                            new AcceptableValueRange<float>(0f, 1f), AdminOnly())),
                };
            }
        }

        internal static string TollItem(BiomeTier tier) =>
            ResolvedTollItem.TryGetValue(tier, out var resolved)
                ? resolved
                : PerTier.TryGetValue(tier, out var e) ? e.TollItem.Value : null;

        /// <summary>
        /// Checks every configured toll item against the item database once the game has
        /// loaded, and falls back to the shipped default for any that does not exist.
        ///
        /// A typo here is otherwise unrecoverable in play: the count of a nonexistent item
        /// is always zero, so the toll can never be paid and that biome's cargo simply
        /// stops crossing, with nothing on screen to explain why. That is a server admin's
        /// mistake rather than an exploit, so it recovers loudly rather than failing shut.
        /// </summary>
        internal static void ResolveTollItems()
        {
            ResolvedTollItem.Clear();

            foreach (var tier in Tiers.All)
            {
                if (!PerTier.TryGetValue(tier.Tier, out var entries))
                {
                    continue;
                }

                var configured = entries.TollItem.Value;

                if (Exists(configured))
                {
                    ResolvedTollItem[tier.Tier] = configured;
                    continue;
                }

                if (Exists(tier.DefaultTollItem))
                {
                    ResolvedTollItem[tier.Tier] = tier.DefaultTollItem;
                    TollboundPlugin.LogError(
                        $"Tier - {tier.Tier}: TollItem '{configured}' is not an item in this " +
                        $"install. Falling back to '{tier.DefaultTollItem}'. Fix the config, " +
                        "or that biome's toll will not be what you intended.");
                    continue;
                }

                ResolvedTollItem[tier.Tier] = null;
                TollboundPlugin.LogError(
                    $"Tier - {tier.Tier}: neither TollItem '{configured}' nor the default " +
                    $"'{tier.DefaultTollItem}' exists. This biome is now toll-free.");
            }
        }

        private static bool Exists(string prefabName) =>
            !string.IsNullOrEmpty(prefabName)
            && ObjectDB.instance != null
            && ObjectDB.instance.TryGetItemPrefab(prefabName, out var go)
            && go != null;

        internal static int TollAmount(BiomeTier tier) =>
            PerTier.TryGetValue(tier, out var e) ? e.TollAmount.Value : 0;

        internal static float LossRate(BiomeTier tier) =>
            PerTier.TryGetValue(tier, out var e) ? e.LossRate.Value : 0f;

        internal static int LoadSize(BiomeTier tier) =>
            PerTier.TryGetValue(tier, out var e) ? e.LoadSize.Value : 30;

        private static ConfigurationManagerAttributes AdminOnly() =>
            new ConfigurationManagerAttributes { IsAdminOnly = true };
    }
}
