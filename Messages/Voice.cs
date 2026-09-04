using System.Collections.Generic;
using Tollbound.Gameplay;
using Tollbound.Model;

namespace Tollbound.Messages
{
    /// <summary>
    /// Everything the player is told, on two channels: the reason goes centre-screen where
    /// it interrupts, and the arithmetic goes to the top-left queue where it can be read at
    /// leisure.
    ///
    /// Deliberately plain for now. The clause-composed spirit dialogue slots in behind
    /// these same two entry points later, so nothing else has to change: callers never
    /// build a sentence, and the ledger lines stay generated rather than authored.
    ///
    /// Counts are written "Iron x3" rather than pluralized, which sidesteps English plural
    /// rules entirely and stays correct once this is localized.
    /// </summary>
    internal static class Voice
    {
        internal static void Refused(Player player, Verdict verdict)
        {
            switch (verdict.Refusal)
            {
                case Refusal.AboveCeiling:
                    Centre(player, "The way will not take it.");
                    TopLeft(player, CeilingExplanation(verdict));
                    break;

                case Refusal.TollUnpaid:
                    var toll = verdict.Unaffordable;
                    var spirit = SpiritOf(toll.Tier);
                    Centre(player, $"{Capitalize(spirit)} demands payment.");
                    TopLeft(player,
                        $"{Capitalize(spirit)} demands {Item(toll.ItemPrefab)} x{toll.Amount}. " +
                        $"You carry {toll.Held}.");
                    break;
            }
        }

        internal static void Crossed(Player player, Verdict verdict, List<Loss> losses)
        {
            foreach (var toll in verdict.Tolls)
            {
                TopLeft(player,
                    $"{Item(toll.ItemPrefab)} x{toll.Amount} - paid to {SpiritOf(toll.Tier)}");
            }

            foreach (var loss in losses)
            {
                TopLeft(player,
                    $"{Item(loss.Prefab)} x{loss.Count} - claimed by {SpiritOf(loss.Tier)}");
            }
        }

        private static string CeilingExplanation(Verdict verdict)
        {
            var item = Item(verdict.OffendingItem);

            // A crossing with a vanilla portal on either end carries nothing restricted,
            // so naming a biome would be misleading.
            if (verdict.Ceiling == BiomeTier.None)
            {
                return $"This crossing will not carry {item}.";
            }

            var ceiling = Tiers.Get(verdict.Ceiling);
            var offending = Tiers.Get(verdict.OffendingTier);

            return $"A {ceiling.BiomeName} crossing will not carry {item} " +
                   $"({offending.BiomeName}).";
        }

        private static string SpiritOf(BiomeTier tier)
        {
            var info = Tiers.Get(tier);
            return info == null ? "the spirits" : info.SpiritName;
        }

        private static string Item(string prefabName) => PortalIdentity.DisplayName(prefabName);

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static void Centre(Player player, string text) =>
            player.Message(MessageHud.MessageType.Center, text);

        private static void TopLeft(Player player, string text) =>
            player.Message(MessageHud.MessageType.TopLeft, text);
    }
}
