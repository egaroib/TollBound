using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Tollbound.Model;

namespace Tollbound.Messages
{
    internal enum VoiceSlot
    {
        Opener,
        Refused,
        Unpaid,
        Toll,
    }

    /// <summary>
    /// The authored lines, loaded from BepInEx/config/Tollbound/voice.txt.
    ///
    /// A sectioned text file rather than JSON on purpose: this exists to be hand-edited,
    /// and JSON's escaping and trailing commas turn a small addition into a syntax error
    /// that silences the whole voice. Here an unparseable line is just a line.
    ///
    /// Lookup falls back from a spirit to [fallback], so an unauthored biome speaks in the
    /// generic register until someone adds a section for it. Adding one needs no rebuild.
    /// </summary>
    internal static class VoiceBook
    {
        private const string FileName = "voice.txt";

        /// <summary>How many recent picks to avoid, so the same line never lands twice running.</summary>
        private const int MemoryDepth = 3;

        private static readonly Dictionary<string, List<string>> Pools =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Queue<string>> Recent =
            new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsLoaded { get; private set; }

        internal static void Load()
        {
            try
            {
                var path = Path.Combine(Path.Combine(Paths.ConfigPath, "Tollbound"), FileName);

                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, DefaultVoice.Text);
                    TollboundPlugin.LogInfo($"Wrote default spirit lines to {path}");
                }

                Parse(File.ReadAllText(path));
                MergeNewSections(path);
                IsLoaded = true;

                var lines = 0;
                foreach (var pool in Pools.Values)
                {
                    lines += pool.Count;
                }

                TollboundPlugin.LogInfo(
                    $"Spirit voice: {lines} line(s) across {Pools.Count} pool(s).");
            }
            catch (Exception e)
            {
                // Dialogue is decoration. A broken file must not stop anyone teleporting.
                TollboundPlugin.LogError($"Could not load spirit lines, falling back to silence: {e.Message}");
                IsLoaded = false;
            }
        }

        /// <summary>
        /// Adds any section the shipped defaults have and the player's file does not.
        ///
        /// The file is written once and never overwritten, so hand edits survive updates —
        /// but that also means a spirit voiced in a later version would never reach anyone
        /// who already had a file. Merging by whole section keeps both: existing sections
        /// are left exactly as the player left them, and only genuinely new ones are added.
        ///
        /// The cost is that deleting a section to silence a spirit will not stick. Emptying
        /// it does, which the file header explains.
        /// </summary>
        private static void MergeNewSections(string path)
        {
            var missing = new List<string>();
            var appended = new List<string>();
            string key = null;

            foreach (var raw in DefaultVoice.Text.Split('\n'))
            {
                var line = raw.Trim();

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    key = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();

                    if (Pools.ContainsKey(key))
                    {
                        key = null;
                        continue;
                    }

                    missing.Add(key);
                    appended.Add("");
                    appended.Add("[" + key + "]");
                    Pools[key] = new List<string>();
                    continue;
                }

                if (key != null)
                {
                    appended.Add(line);
                    Pools[key].Add(line);
                }
            }

            if (missing.Count == 0)
            {
                return;
            }

            File.AppendAllText(path,
                "\n\n# Added by a Tollbound update. Edit freely; this file is never rewritten.\n"
                + string.Join("\n", appended.ToArray()) + "\n");

            TollboundPlugin.LogInfo(
                $"Added {missing.Count} new spirit section(s) to voice.txt: " +
                string.Join(", ", missing.ToArray()));
        }

        private static void Parse(string text)
        {
            Pools.Clear();
            Recent.Clear();

            string key = null;

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    key = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }

                if (key == null)
                {
                    continue;
                }

                if (!Pools.TryGetValue(key, out var pool))
                {
                    pool = new List<string>();
                    Pools[key] = pool;
                }

                pool.Add(line);
            }
        }

        /// <summary>
        /// One line for this spirit, mood and slot, avoiding the last few picked. Returns
        /// null when nothing is authored anywhere, which callers treat as "say nothing"
        /// rather than as an error.
        /// </summary>
        internal static string Line(BiomeTier tier, bool sated, VoiceSlot slot)
        {
            if (!IsLoaded)
            {
                return null;
            }

            var mood = sated ? "sated" : "restless";
            var name = slot.ToString().ToLowerInvariant();

            return Pick($"{SpiritKey(tier)}.{mood}.{name}")
                   ?? Pick($"fallback.{mood}.{name}");
        }

        private static string Pick(string key)
        {
            if (!Pools.TryGetValue(key, out var pool) || pool.Count == 0)
            {
                return null;
            }

            if (!Recent.TryGetValue(key, out var recent))
            {
                recent = new Queue<string>();
                Recent[key] = recent;
            }

            // Never exclude the whole pool: a two-line section must stay usable.
            var avoid = Math.Min(MemoryDepth, pool.Count - 1);

            string choice = null;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidate = pool[UnityEngine.Random.Range(0, pool.Count)];
                if (avoid <= 0 || !recent.Contains(candidate))
                {
                    choice = candidate;
                    break;
                }
            }

            if (choice == null)
            {
                choice = pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            recent.Enqueue(choice);
            while (recent.Count > avoid)
            {
                recent.Dequeue();
            }

            return choice;
        }

        private static string SpiritKey(BiomeTier tier) => tier.ToString().ToLowerInvariant();
    }
}
