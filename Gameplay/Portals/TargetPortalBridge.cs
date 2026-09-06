using System;
using System.Reflection;
using HarmonyLib;

namespace Tollbound.Gameplay.Portals
{
    /// <summary>
    /// Brings map-based portal travel under Tollbound's rules.
    ///
    /// TargetPortal opens the map when you step into a portal and warps you to any pin, and
    /// it does so by calling Player.TeleportTo directly — TeleportWorld.Teleport never runs,
    /// so the gate patched there never sees the crossing.
    ///
    /// Rather than replace any of TargetPortal's logic, this brackets its click handler with
    /// a flag. The real work happens in MapTeleportPatch, on vanilla's own TeleportTo, which
    /// means the same mechanism would catch any other mod that warps a player away from a
    /// portal the same way.
    /// </summary>
    internal static class TargetPortalBridge
    {
        internal const string Guid = "org.bepinex.plugins.targetportal";

        private static bool _bound;

        /// <summary>
        /// True only for the moment TargetPortal is acting on a click. Everything else that
        /// calls TeleportTo — respawns, debug commands, Tollbound's own crossings — happens
        /// with this false and is left alone.
        /// </summary>
        internal static bool MapCrossingInProgress { get; private set; }

        internal static bool Loaded { get; private set; }

        internal static void Bind(Harmony harmony)
        {
            if (_bound)
            {
                return;
            }

            _bound = true;

            var map = FindType("TargetPortal.Map");
            if (map == null)
            {
                return;
            }

            try
            {
                var target = AccessTools.Method(map, "HandlePortalClick");
                if (target == null)
                {
                    TollboundPlugin.LogWarning(
                        "TargetPortal is installed but HandlePortalClick was not found. " +
                        "Map travel will fall back to its own restrictions, which block all " +
                        "restricted cargo rather than applying Tollbound's rules.");
                    return;
                }

                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(TargetPortalBridge), nameof(Open)),
                    finalizer: new HarmonyMethod(typeof(TargetPortalBridge), nameof(Close)));

                Loaded = true;
                TollboundPlugin.LogInfo(
                    "TargetPortal detected. Map travel from a biome portal is now tolled.");
            }
            catch (Exception e)
            {
                TollboundPlugin.LogWarning(
                    $"Could not hook TargetPortal ({e.Message}). Map travel will fall back " +
                    "to its own restrictions.");
            }
        }

        // Parameterless on purpose: the click handler takes a delegate of a type private to
        // TargetPortal, and nothing here needs it. The destination is read from the
        // teleport itself instead.
        private static void Open() => MapCrossingInProgress = true;

        // A finalizer rather than a postfix, so the flag clears even if their handler throws.
        private static void Close() => MapCrossingInProgress = false;

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName, throwOnError: false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // A malformed assembly elsewhere in the profile is not our problem.
                }
            }

            return null;
        }
    }
}
