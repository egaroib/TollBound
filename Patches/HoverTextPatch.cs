using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Tollbound.Config;
using Tollbound.Gameplay;
using Tollbound.Model;

namespace Tollbound.Patches
{
    /// <summary>
    /// Tells the player what this particular crossing will carry and cost, before they walk
    /// into it.
    ///
    /// Without this the only way to learn a rule is to be refused by it, which is a poor way
    /// to teach a system with six tiers and a ceiling that depends on both ends of the link.
    /// The far end's tier is named explicitly, because a Swamp portal wired to a Black
    /// Forest one looks identical to one wired to another Swamp portal.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    internal static class HoverTextPatch
    {
        private const string Warn = "#E0A34E";
        private const string Bad = "#E06C5C";
        private const string Faint = "#9AA7A9";

        private static void Postfix(TeleportWorld __instance, ref string __result)
        {
            var near = PortalIdentity.TierOf(__instance);
            if (near == BiomeTier.None)
            {
                return;
            }

            var lines = new List<string>();
            var connected = __instance.HaveTarget();
            var far = connected ? PortalIdentity.TierOfFarEnd(__instance) : BiomeTier.None;

            lines.Add(RouteLine(near, far, connected));

            if (connected)
            {
                var cargo = CargoLine(PortalIdentity.Ceiling(near, far));
                if (cargo != null)
                {
                    lines.Add(cargo);
                }
            }

            var sb = new StringBuilder(__result);
            foreach (var line in lines)
            {
                sb.Append("\n").Append(line);
            }

            __result = sb.ToString();
        }

        private static string RouteLine(BiomeTier near, BiomeTier far, bool connected)
        {
            var nearName = NameOf(near);

            if (!connected)
            {
                return Dim($"Carries {nearName} cargo and below, once linked.");
            }

            if (far == near)
            {
                return $"{nearName} crossing.";
            }

            // A vanilla portal on the far end is worth naming as such rather than as a
            // missing biome, since it is a perfectly ordinary thing to link to.
            var farName = far == BiomeTier.None ? "a plain portal" : NameOf(far);
            var ceiling = PortalIdentity.Ceiling(near, far);

            var carries = ceiling == BiomeTier.None
                ? "Carries nothing restricted."
                : $"Carries {NameOf(ceiling)} cargo and below.";

            return $"{nearName} to {farName}. {carries}";
        }

        /// <summary>
        /// What the player's current pack means for this crossing: the price, the blocker,
        /// or the metal a living boss may take. Null when they carry nothing restricted.
        /// </summary>
        private static string CargoLine(BiomeTier ceiling)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return null;
            }

            var manifest = Manifest.Cached(player);
            if (manifest.IsEmpty)
            {
                return null;
            }

            if (manifest.Unrecognized.Count > 0)
            {
                return Colour(Bad,
                    $"{PortalIdentity.DisplayName(manifest.Unrecognized[0])} cannot cross here.");
            }

            var blocked = manifest.Lots
                .Where(l => l.Tier > ceiling)
                .OrderByDescending(l => l.Tier)
                .FirstOrDefault();

            if (blocked != null)
            {
                return Colour(Bad,
                    $"Refused: {PortalIdentity.DisplayName(blocked.Prefab)} " +
                    $"({NameOf(blocked.Tier)} cargo).");
            }

            var parts = new List<string>();

            var toll = TollSummary(manifest, player);
            if (toll != null)
            {
                parts.Add(toll);
            }

            var risk = RiskSummary(manifest);
            if (risk != null)
            {
                parts.Add(risk);
            }

            return parts.Count == 0 ? null : string.Join("  ", parts.ToArray());
        }

        private static string TollSummary(Manifest manifest, Player player)
        {
            var owed = new List<string>();
            var short_ = false;

            foreach (var tier in manifest.TiersPresent)
            {
                // Priced through the gate's own method, so the quote and the charge cannot
                // drift apart when scaling changes.
                var toll = TollGate.Price(tier, manifest.UnitsOf(tier));
                if (toll == null)
                {
                    continue;
                }

                if (Cargo.Count(player, toll.ItemPrefab) < toll.Amount)
                {
                    short_ = true;
                }

                var entry = $"{PortalIdentity.DisplayName(toll.ItemPrefab)} x{toll.Amount}";
                owed.Add(toll.Loads > 1 ? entry + $" ({toll.Loads} loads)" : entry);
            }

            if (owed.Count == 0)
            {
                return null;
            }

            var text = "Toll: " + string.Join(", ", owed.ToArray());
            return short_ ? Colour(Bad, text + " (short)") : text;
        }

        private static string RiskSummary(Manifest manifest)
        {
            var atRisk = new List<string>();

            foreach (var tier in manifest.TiersPresent)
            {
                if (TollGate.BossIsDead(tier))
                {
                    continue;
                }

                var rate = TollboundConfig.LossRate(tier);
                if (rate <= 0f)
                {
                    continue;
                }

                var units = manifest.Of(tier).Where(l => l.Destructible).Sum(l => l.Count);
                if (units <= 0)
                {
                    continue;
                }

                // The expected loss, not just the rate: a percentage alone does not tell
                // you whether to risk the trip, and it is the number worth tuning against.
                var expected = units * rate;
                var likely = expected < 0.5f ? "under 1" : $"~{UnityEngine.Mathf.RoundToInt(expected)}";

                atRisk.Add($"{units} at {rate * 100f:0.#}% to {SpiritOf(tier)} ({likely})");
            }

            return atRisk.Count == 0
                ? null
                : Colour(Warn, "At risk: " + string.Join(", ", atRisk.ToArray()));
        }

        private static string NameOf(BiomeTier tier)
        {
            var info = Tiers.Get(tier);
            return info == null ? "nothing" : info.BiomeName;
        }

        private static string SpiritOf(BiomeTier tier)
        {
            var info = Tiers.Get(tier);
            return info == null ? "the spirits" : info.SpiritName;
        }

        private static string Colour(string hex, string text) =>
            $"<color={hex}>{text}</color>";

        private static string Dim(string text) => Colour(Faint, text);
    }
}
