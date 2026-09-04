using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;

namespace Tollbound.Diagnostics
{
    /// <summary>
    /// A one-shot survey of what this install actually contains, written to disk on the
    /// first game start.
    ///
    /// This exists because the facts Tollbound needs are not readable from the decompiled
    /// source: item flags such as m_teleportable live on prefabs inside compressed asset
    /// bundles, and so do the portal prefab's own settings. Guessing prefab names produces
    /// a mod that compiles, loads, binds every patch, and then silently does nothing.
    ///
    /// It also picks up items added by other mods, so the report reflects the player's
    /// real install rather than vanilla in isolation.
    /// </summary>
    internal static class ItemReport
    {
        /// <summary>
        /// Prefab names this mod's design depends on. Every one of these is currently an
        /// assumption; the report says which ones are real. Grouped so a missing entry
        /// points straight at the design decision it would break.
        /// </summary>
        private static readonly List<KeyValuePair<string, string[]>> Candidates =
            new List<KeyValuePair<string, string[]>>
        {
            new KeyValuePair<string, string[]>("Toll items", new[]
            {
                "GreydwarfEye", "Entrails", "FreezeGland", "Needle", "Bilebag", "CharredBone",
            }),
            new KeyValuePair<string, string[]>("Portal recipe - metal", new[]
            {
                "Copper", "Iron", "Silver", "BlackMetal", "Eitr", "Flametal", "FlametalNew",
            }),
            new KeyValuePair<string, string[]>("Portal recipe - frame", new[]
            {
                "FineWood", "YggdrasilWood", "BlackMarble", "Blackwood", "Grausten",
                "AncientBark", "Obsidian", "SurtlingCore",
            }),
            new KeyValuePair<string, string[]>("Restricted cargo - assumed", new[]
            {
                "CopperOre", "TinOre", "Tin", "Bronze", "IronOre", "IronScrap",
                "SilverOre", "BlackMetalScrap", "FlametalOre", "FlametalOreNew",
            }),
            new KeyValuePair<string, string[]>("Named in design, existence unconfirmed", new[]
            {
                "BronzeScrap", "CopperScrap", "DragonEgg", "MechanicalSpring",
                "CharredCogwheel", "DvergrKeyFragment",
            }),
        };

        /// <summary>Vanilla portals. Tollbound clones one and must never patch the other.</summary>
        private static readonly string[] PortalPrefabs = { "portal_wood", "portal_stone" };

        private static bool _hasRun;

        internal static void Run()
        {
            if (_hasRun)
            {
                return;
            }

            try
            {
                if (ObjectDB.instance == null)
                {
                    TollboundPlugin.LogWarning("ItemReport: ObjectDB not ready, skipping.");
                    return;
                }

                var path = Write();
                _hasRun = true;
                TollboundPlugin.LogInfo($"Item report written to {path}");
            }
            catch (Exception e)
            {
                // A diagnostic must never take the mod down with it.
                TollboundPlugin.LogError($"ItemReport failed: {e}");
            }
        }

        private static string Write()
        {
            var restricted = new List<ItemDrop.ItemData.SharedData>();
            var byPrefab = new Dictionary<string, ItemDrop.ItemData.SharedData>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var go in ObjectDB.instance.m_items)
            {
                if (go == null)
                {
                    continue;
                }

                var drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData?.m_shared == null)
                {
                    continue;
                }

                var shared = drop.m_itemData.m_shared;
                byPrefab[go.name] = shared;

                if (!shared.m_teleportable)
                {
                    restricted.Add(shared);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Tollbound item report");
            sb.AppendLine();
            sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}  ");
            sb.AppendLine($"Tollbound v{TollboundPlugin.PluginVersion}  ");
            sb.AppendLine($"{ObjectDB.instance.m_items.Count} item prefabs in ObjectDB, " +
                          $"{restricted.Count} of them non-teleportable.");
            sb.AppendLine();

            AppendRestricted(sb, restricted, byPrefab);
            AppendCandidates(sb, byPrefab);
            AppendPortals(sb);

            var dir = Path.Combine(Paths.ConfigPath, "Tollbound");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "item-report.md");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static void AppendRestricted(
            StringBuilder sb,
            List<ItemDrop.ItemData.SharedData> restricted,
            Dictionary<string, ItemDrop.ItemData.SharedData> byPrefab)
        {
            sb.AppendLine("## Non-teleportable items");
            sb.AppendLine();
            sb.AppendLine("Everything vanilla (and any other loaded mod) marks " +
                          "`m_teleportable = false`. This is the candidate set for " +
                          "Tollbound's cargo tiers.");
            sb.AppendLine();
            sb.AppendLine("The last two columns are Tollbound's own classification, so a " +
                          "`none` tier here is an item that would cross a biome portal " +
                          "untolled.");
            sb.AppendLine();
            sb.AppendLine("| Prefab | Name | Type | Stack | Tier | Can be lost |");
            sb.AppendLine("|---|---|---|---|---|---|");

            // Invert the lookup so each restricted SharedData can report its prefab name,
            // which is what config files are keyed on.
            var prefabOf = new Dictionary<ItemDrop.ItemData.SharedData, string>();
            foreach (var kv in byPrefab)
            {
                prefabOf[kv.Value] = kv.Key;
            }

            var rows = restricted
                .Select(s =>
                {
                    var prefab = prefabOf.TryGetValue(s, out var p) ? p : "?";
                    var tier = Model.CargoRegistry.TierOfItem(prefab);
                    return new
                    {
                        Prefab = prefab,
                        Name = Localize(s.m_name),
                        Type = s.m_itemType.ToString(),
                        Stack = s.m_maxStackSize,
                        Tier = tier == Model.BiomeTier.None ? "**none**" : tier.ToString(),
                        Lost = Model.CargoRegistry.CanBeLost(prefab) ? "yes" : "protected",
                    };
                })
                .OrderBy(r => r.Prefab, StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                sb.AppendLine($"| `{r.Prefab}` | {r.Name} | {r.Type} | {r.Stack} | " +
                              $"{r.Tier} | {r.Lost} |");
            }

            sb.AppendLine();
        }

        private static void AppendCandidates(
            StringBuilder sb,
            Dictionary<string, ItemDrop.ItemData.SharedData> byPrefab)
        {
            sb.AppendLine("## Design assumptions");
            sb.AppendLine();
            sb.AppendLine("Prefab names the spec currently relies on. A `MISSING` row is a " +
                          "design decision that needs a different item.");
            sb.AppendLine();

            foreach (var group in Candidates)
            {
                sb.AppendLine($"### {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| Prefab | Found | Name | Teleportable |");
                sb.AppendLine("|---|---|---|---|");

                foreach (var name in group.Value)
                {
                    if (byPrefab.TryGetValue(name, out var shared))
                    {
                        var tele = shared.m_teleportable ? "yes" : "**no**";
                        sb.AppendLine($"| `{name}` | ok | {Localize(shared.m_name)} | {tele} |");
                    }
                    else
                    {
                        sb.AppendLine($"| `{name}` | **MISSING** | — | — |");
                    }
                }

                sb.AppendLine();
            }
        }

        private static void AppendPortals(StringBuilder sb)
        {
            sb.AppendLine("## Vanilla portals");
            sb.AppendLine();
            sb.AppendLine("Tollbound clones `portal_wood` and must leave both of these alone. " +
                          "`m_allowAllItems` is the flag that decides whether vanilla runs its " +
                          "own teleport check at all.");
            sb.AppendLine();

            foreach (var name in PortalPrefabs)
            {
                var prefab = PrefabManager.Instance?.GetPrefab(name);
                if (prefab == null)
                {
                    sb.AppendLine($"- `{name}` — **MISSING**");
                    continue;
                }

                var tp = prefab.GetComponent<TeleportWorld>();
                if (tp == null)
                {
                    sb.AppendLine($"- `{name}` — found, but has no TeleportWorld component");
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                var pieceName = piece != null ? Localize(piece.m_name) : "(no Piece)";

                sb.AppendLine($"- `{name}` — {pieceName}  ");
                sb.AppendLine($"  - `m_allowAllItems` = **{tp.m_allowAllItems}**");
                sb.AppendLine($"  - `m_activationRange` = {tp.m_activationRange}, " +
                              $"`m_exitDistance` = {tp.m_exitDistance}");
                sb.AppendLine($"  - unconnected {ToHex(tp.m_colorUnconnected)}, " +
                              $"target-found {ToHex(tp.m_colorTargetfound)}");
                sb.AppendLine($"  - effect lists: connected " +
                              $"{tp.m_connected?.m_effectPrefabs?.Length ?? 0}, " +
                              $"model {(tp.m_model != null ? "present" : "null")}");

                if (piece?.m_resources != null)
                {
                    var cost = string.Join(", ", piece.m_resources
                        .Where(r => r?.m_resItem != null)
                        .Select(r => $"{r.m_resItem.name} x{r.m_amount}"));
                    sb.AppendLine($"  - build cost: {cost}");
                }
            }

            sb.AppendLine();
        }

        private static string Localize(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "";
            }

            return Localization.instance != null
                ? Localization.instance.Localize(token)
                : token;
        }

        private static string ToHex(Color c) =>
            $"#{ColorUtility.ToHtmlStringRGB(c)} (a={c.a:0.##}, hdr max={Mathf.Max(c.r, c.g, c.b):0.##})";
    }
}
