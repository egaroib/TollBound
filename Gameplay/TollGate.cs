using System.Collections.Generic;
using System.Linq;
using Tollbound.Config;
using Tollbound.Model;

namespace Tollbound.Gameplay
{
    internal enum Refusal
    {
        None = 0,

        /// <summary>Carrying something above what this crossing admits.</summary>
        AboveCeiling,

        /// <summary>A spirit's toll is not in the pack.</summary>
        TollUnpaid,

        /// <summary>Carrying something the game restricts that Tollbound has no tier for.</summary>
        Unrecognized,
    }

    /// <summary>A toll owed to one biome's spirit for this crossing.</summary>
    internal sealed class TollDue
    {
        internal BiomeTier Tier;
        internal string ItemPrefab;

        /// <summary>The configured price of a single load.</summary>
        internal int BaseAmount;

        /// <summary>Loads being charged for. Always 1 under flat scaling.</summary>
        internal int Loads = 1;

        /// <summary>What the crossing actually costs: BaseAmount multiplied by Loads.</summary>
        internal int Amount;

        internal int Held;

        internal bool Affordable => Held >= Amount;
    }

    /// <summary>Units of one item destroyed in transit.</summary>
    internal sealed class Loss
    {
        internal BiomeTier Tier;
        internal string Prefab;
        internal int Count;
    }

    internal sealed class Verdict
    {
        internal bool Allowed;
        internal Refusal Refusal = Refusal.None;
        internal BiomeTier Ceiling;

        /// <summary>Set when Refusal is AboveCeiling.</summary>
        internal string OffendingItem;
        internal BiomeTier OffendingTier;

        /// <summary>Set when Refusal is TollUnpaid.</summary>
        internal TollDue Unaffordable;

        /// <summary>Set when Refusal is Unrecognized.</summary>
        internal string UnrecognizedItem;

        internal readonly List<TollDue> Tolls = new List<TollDue>();
        internal Manifest Manifest;
    }

    /// <summary>
    /// The whole rule, in one place and independent of how the crossing was initiated.
    /// Vanilla portals call in through TeleportWorld.Teleport; a map-based portal mod would
    /// call in the same way with its own two endpoints.
    ///
    /// Evaluate never mutates anything. Apply is only reached once every check has passed,
    /// so a refused crossing costs the player nothing.
    /// </summary>
    internal static class TollGate
    {
        internal static Verdict Evaluate(Player player, BiomeTier near, BiomeTier far)
        {
            var ceiling = PortalIdentity.Ceiling(near, far);
            var verdict = new Verdict
            {
                Ceiling = ceiling,
                Manifest = Manifest.Build(player == null ? null : player.GetInventory()),
            };

            if (verdict.Manifest.IsEmpty)
            {
                verdict.Allowed = true;
                return verdict;
            }

            // Fail closed on anything the game restricts but Tollbound cannot price. A
            // game update adding a new ore should refuse, not quietly travel free.
            if (verdict.Manifest.Unrecognized.Count > 0)
            {
                verdict.Refusal = Refusal.Unrecognized;
                verdict.UnrecognizedItem = verdict.Manifest.Unrecognized[0];
                return verdict;
            }

            // Ceiling first: no point pricing a crossing that cannot happen.
            var offending = verdict.Manifest.Lots
                .Where(l => l.Tier > ceiling)
                .OrderByDescending(l => l.Tier)
                .FirstOrDefault();

            if (offending != null)
            {
                verdict.Refusal = Refusal.AboveCeiling;
                verdict.OffendingItem = offending.Prefab;
                verdict.OffendingTier = offending.Tier;
                return verdict;
            }

            var inventory = player.GetInventory();

            foreach (var tier in verdict.Manifest.TiersPresent)
            {
                var toll = Price(tier, verdict.Manifest.UnitsOf(tier));
                if (toll == null)
                {
                    continue;
                }

                toll.Held = Cargo.Count(inventory, toll.ItemPrefab);
                verdict.Tolls.Add(toll);
            }

            // Every toll must be payable before any of it is spent, so a player is never
            // charged for a crossing that then gets refused.
            var unaffordable = verdict.Tolls.FirstOrDefault(t => !t.Affordable);
            if (unaffordable != null)
            {
                verdict.Refusal = Refusal.TollUnpaid;
                verdict.Unaffordable = unaffordable;
                return verdict;
            }

            verdict.Allowed = true;
            return verdict;
        }

        /// <summary>
        /// What one biome's spirit charges for carrying this much of its cargo. Null when
        /// the biome is configured toll-free.
        ///
        /// Shared by the gate and the portal hover text so the quoted price and the charged
        /// price cannot drift apart.
        /// </summary>
        internal static TollDue Price(BiomeTier tier, int unitsCarried)
        {
            var baseAmount = TollboundConfig.TollAmount(tier);
            var itemPrefab = TollboundConfig.TollItem(tier);

            if (baseAmount <= 0 || string.IsNullOrEmpty(itemPrefab))
            {
                return null;
            }

            var loads = 1;

            if (TollboundConfig.Scaling.Value == TollScaling.PerLoad)
            {
                var loadSize = System.Math.Max(1, TollboundConfig.LoadSize(tier));

                // Round up: a single unit over a stack is still a second load, which is
                // what makes the rule legible as "a toll per stack".
                loads = System.Math.Max(1, (unitsCarried + loadSize - 1) / loadSize);
            }

            return new TollDue
            {
                Tier = tier,
                ItemPrefab = itemPrefab,
                BaseAmount = baseAmount,
                Loads = loads,
                Amount = baseAmount * loads,
            };
        }

        /// <summary>
        /// Takes the tolls and rolls the losses. Only ever called on an allowed verdict.
        /// Returns what was destroyed, for the ledger message.
        /// </summary>
        internal static List<Loss> Apply(Player player, Verdict verdict)
        {
            var inventory = player.GetInventory();

            foreach (var toll in verdict.Tolls)
            {
                Cargo.Remove(inventory, toll.ItemPrefab, toll.Amount);
            }

            var losses = new List<Loss>();

            foreach (var lot in verdict.Manifest.Lots)
            {
                var bossDead = BossIsDead(lot.Tier);
                var rate = TollboundConfig.LossRate(lot.Tier);
                var eligible = lot.Destructible && !bossDead && rate > 0f;
                var lost = eligible ? RollLosses(lot.Count, rate) : 0;

                // Logged for every lot, including skipped ones. "Rolled and came up empty"
                // and "never rolled at all" look identical in game, and at these rates a
                // small haul losing nothing is the most likely single outcome.
                TollboundPlugin.LogVerbose(
                    $"Loss check {lot.Prefab} x{lot.Count} ({lot.Tier}): " +
                    $"destructible={lot.Destructible}, bossDead={bossDead}, rate={rate:0.###} " +
                    $"-> {(eligible ? lost + " lost" : "skipped")}");

                if (lost <= 0)
                {
                    continue;
                }

                var actually = Cargo.Remove(inventory, lot.Prefab, lost);
                if (actually > 0)
                {
                    losses.Add(new Loss { Tier = lot.Tier, Prefab = lot.Prefab, Count = actually });
                }
            }

            return losses;
        }

        /// <summary>
        /// One independent roll per unit, so a large haul loses roughly the configured
        /// share while a small one is genuinely luck.
        /// </summary>
        private static int RollLosses(int units, float rate)
        {
            var lost = 0;

            for (var i = 0; i < units; i++)
            {
                if (UnityEngine.Random.value < rate)
                {
                    lost++;
                }
            }

            return lost;
        }

        internal static bool BossIsDead(BiomeTier tier)
        {
            var info = Tiers.Get(tier);
            if (info == null || string.IsNullOrEmpty(info.BossKey) || ZoneSystem.instance == null)
            {
                return false;
            }

            // String overload throughout: Queen and Fader have no GlobalKeys enum member.
            return ZoneSystem.instance.GetGlobalKey(info.BossKey);
        }
    }
}
