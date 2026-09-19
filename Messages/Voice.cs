using System.Collections.Generic;
using System.Linq;
using Tollbound.Config;
using Tollbound.Gameplay;
using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Messages
{
    /// <summary>
    /// Everything the player is told, on two channels: the spirit speaks centre-screen
    /// where it interrupts, and the arithmetic goes to the top-left queue where it can be
    /// read at leisure.
    ///
    /// The centre line is composed at the CLAUSE level, never the word level — an opener
    /// and a demand, each a complete sentence from its own pool. Word-level assembly is how
    /// you get "The Elder demand 5 Greydwarf eye"; two independent sentences can be shuffled
    /// forever without producing a broken one.
    ///
    /// The authored lines contain no variables at all. Every count lives here, in the
    /// generated ledger, written "Iron x3" rather than pluralized — which sidesteps English
    /// plural rules and keeps translation a matter of whole sentences.
    /// </summary>
    internal static class Voice
    {
        /// <summary>
        /// How long the same refusal stays the same refusal. One walk into a portal fires
        /// TeleportWorld.Teleport dozens of times, and a spirit that says the same thing
        /// forty times over is a bug rather than a character. Comfortably longer than the
        /// burst, comfortably shorter than the time it takes to go and fetch the toll.
        /// </summary>
        private const float RepeatWindow = 5f;

        private static string _lastRefusal;
        private static float _lastRefusalAt = float.NegativeInfinity;

        internal static void Refused(Player player, Verdict verdict)
        {
            if (Repeated(verdict))
            {
                return;
            }

            switch (verdict.Refusal)
            {
                case Refusal.AboveCeiling:
                    Speak(player, verdict.OffendingTier, VoiceSlot.Refused);
                    TopLeft(player, CeilingExplanation(verdict));
                    break;

                case Refusal.TollUnpaid:
                    var toll = verdict.Unaffordable;
                    Speak(player, toll.Tier, VoiceSlot.Unpaid);
                    var loads = toll.Loads > 1
                        ? $" ({toll.Loads} loads at {toll.BaseAmount})"
                        : "";
                    TopLeft(player,
                        $"{Capitalize(SpiritOf(toll.Tier))} demands {Item(toll.ItemPrefab)} " +
                        $"x{toll.Amount}{loads}. You carry {toll.Held}.");
                    break;

                case Refusal.Unrecognized:
                    Speak(player, BiomeTier.None, VoiceSlot.Refused);
                    TopLeft(player,
                        $"{Item(verdict.UnrecognizedItem)} answers to no spirit here. " +
                        "It cannot cross.");
                    break;
            }
        }

        /// <summary>
        /// Whether this is the refusal already showing on screen. Keyed on what the player
        /// would read, so a refusal that changed — a different item, a different price —
        /// always speaks, and only word-for-word repetition is swallowed.
        /// </summary>
        private static bool Repeated(Verdict verdict)
        {
            var key = $"{verdict.Refusal}|{verdict.Ceiling}|{verdict.OffendingItem}|" +
                      $"{verdict.UnrecognizedItem}|{verdict.Unaffordable?.ItemPrefab}|" +
                      $"{verdict.Unaffordable?.Amount}|{verdict.Unaffordable?.Held}";

            if (key == _lastRefusal && Time.time - _lastRefusalAt < RepeatWindow)
            {
                return true;
            }

            // Anchored to what was last said, not to what was last attempted, so walking
            // back in a while later is answered rather than met with silence.
            _lastRefusal = key;
            _lastRefusalAt = Time.time;

            return false;
        }

        internal static void Crossed(Player player, Verdict verdict, List<Loss> losses)
        {
            if (verdict.Tolls.Count > 0 || losses.Count > 0)
            {
                Speak(player, SpeakerFor(verdict, losses), VoiceSlot.Toll);
            }

            foreach (var toll in verdict.Tolls)
            {
                var loads = toll.Loads > 1 ? $" ({toll.Loads} loads)" : "";
                TopLeft(player,
                    $"{Item(toll.ItemPrefab)} x{toll.Amount} - paid to {SpiritOf(toll.Tier)}{loads}");
            }

            foreach (var loss in losses)
            {
                TopLeft(player,
                    $"{Item(loss.Prefab)} x{loss.Count} - claimed by {SpiritOf(loss.Tier)}");
            }
        }

        /// <summary>
        /// Which spirit gets the line when several were paid at once. Whoever took metal
        /// speaks, because that is the part the player will care about; otherwise the
        /// highest tier does, being the furthest the crossing reached.
        /// </summary>
        private static BiomeTier SpeakerFor(Verdict verdict, List<Loss> losses)
        {
            if (losses.Count > 0)
            {
                return losses.OrderByDescending(l => l.Count).First().Tier;
            }

            return verdict.Tolls.Count > 0
                ? verdict.Tolls.OrderByDescending(t => (int)t.Tier).First().Tier
                : BiomeTier.None;
        }

        /// <summary>
        /// One opener plus one demand, both complete sentences, both authored without
        /// variables. Mood follows whether that biome's boss is still standing.
        /// </summary>
        private static void Speak(Player player, BiomeTier tier, VoiceSlot slot)
        {
            if (!TollboundConfig.SpiritDialogue.Value)
            {
                return;
            }

            var sated = tier != BiomeTier.None && TollGate.BossIsDead(tier);

            var opener = VoiceBook.Line(tier, sated, VoiceSlot.Opener);
            var demand = VoiceBook.Line(tier, sated, slot);

            var line = string.IsNullOrEmpty(opener) ? demand : $"{opener} {demand}";

            if (!string.IsNullOrEmpty(line))
            {
                player.Message(MessageHud.MessageType.Center, line);
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

            return $"A {ceiling.BiomeName} crossing will not carry {item} ({offending.BiomeName}).";
        }

        private static string SpiritOf(BiomeTier tier)
        {
            var info = Tiers.Get(tier);
            return info == null ? "the spirits" : info.SpiritName;
        }

        private static string Item(string prefabName) => PortalIdentity.DisplayName(prefabName);

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static void TopLeft(Player player, string text) =>
            player.Message(MessageHud.MessageType.TopLeft, text);
    }
}
