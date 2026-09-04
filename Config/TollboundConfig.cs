using System.Collections.Generic;
using BepInEx.Configuration;
using Tollbound.Model;

namespace Tollbound.Config
{
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

        internal static ConfigEntry<bool> SpiritDialogue;

        internal static ConfigEntry<float> IdleGlowIntensity;
        internal static ConfigEntry<float> ConnectedGlowIntensity;

        private sealed class TierEntries
        {
            internal ConfigEntry<string> TollItem;
            internal ConfigEntry<int> TollAmount;
            internal ConfigEntry<float> LossRate;
        }

        private static readonly Dictionary<BiomeTier, TierEntries> PerTier =
            new Dictionary<BiomeTier, TierEntries>();

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
            PerTier.TryGetValue(tier, out var e) ? e.TollItem.Value : null;

        internal static int TollAmount(BiomeTier tier) =>
            PerTier.TryGetValue(tier, out var e) ? e.TollAmount.Value : 0;

        internal static float LossRate(BiomeTier tier) =>
            PerTier.TryGetValue(tier, out var e) ? e.LossRate.Value : 0f;

        private static ConfigurationManagerAttributes AdminOnly() =>
            new ConfigurationManagerAttributes { IsAdminOnly = true };
    }
}
