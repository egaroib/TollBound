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
    }

    /// <summary>A toll owed to one biome's spirit for this crossing.</summary>
    internal sealed class TollDue
    {
        internal BiomeTier Tier;
        internal string ItemPrefab;
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
                var amount = TollboundConfig.TollAmount(tier);
                var itemPrefab = TollboundConfig.TollItem(tier);

                if (amount <= 0 || string.IsNullOrEmpty(itemPrefab))
                {
                    continue;
                }

                verdict.Tolls.Add(new TollDue
                {
                    Tier = tier,
                    ItemPrefab = itemPrefab,
                    Amount = amount,
                    Held = Cargo.Count(inventory, itemPrefab),
                });
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
                if (!lot.Destructible || BossIsDead(lot.Tier))
                {
                    continue;
                }

                var rate = TollboundConfig.LossRate(lot.Tier);
                if (rate <= 0f)
                {
                    continue;
                }

                var lost = RollLosses(lot.Count, rate);
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
