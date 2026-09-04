using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Tollbound.Config;
using Tollbound.Model;
using UnityEngine;

namespace Tollbound.Content
{
    /// <summary>
    /// Creates the six biome portals by cloning portal_wood and re-dressing it.
    ///
    /// Tier 2 work throughout: no custom models, no AssetBundle. What changes per biome is
    /// the two HDR emission colours on TeleportWorld, the build cost, and the crafting
    /// station. The silhouette stays vanilla's, which is the honest limit of what can be
    /// built without the Unity Editor.
    /// </summary>
    internal static class PortalPieces
    {
        private const string BasePortal = "portal_wood";

        private static bool _registered;

        internal static void Register()
        {
            if (_registered)
            {
                return;
            }

            if (PrefabManager.Instance.GetPrefab(BasePortal) == null)
            {
                TollboundPlugin.LogError(
                    $"'{BasePortal}' not found. No biome portals were created. " +
                    "This is a game-version problem, not a config one.");
                return;
            }

            var created = 0;

            foreach (var tier in Tiers.All)
            {
                if (TryCreate(tier))
                {
                    created++;
                }
            }

            _registered = true;
            TollboundPlugin.LogInfo($"Registered {created} of {Tiers.All.Length} biome portals.");
        }

        private static bool TryCreate(TierInfo tier)
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(tier.PortalPrefab, BasePortal);
            if (prefab == null)
            {
                TollboundPlugin.LogError($"Could not clone {BasePortal} for {tier.DisplayName}.");
                return false;
            }

            Tint(prefab, tier);

            var config = new PieceConfig
            {
                Name = tier.NameToken,
                Description = tier.NameToken + "_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = tier.CraftingStation,
                Requirements = tier.Recipe
                    .Select(r => new RequirementConfig(r.Item, r.Amount))
                    .ToArray(),
            };

            // fixReference: false because the clone's references already point at real
            // game objects rather than Jotunn mocks.
            var piece = new CustomPiece(prefab, fixReference: false, config);
            if (!piece.IsValid())
            {
                TollboundPlugin.LogError($"{tier.DisplayName} failed validation and was skipped.");
                return false;
            }

            if (!PieceManager.Instance.AddPiece(piece))
            {
                TollboundPlugin.LogError($"{tier.DisplayName} was rejected by the PieceManager.");
                return false;
            }

            AddLocalization(tier);
            TollboundPlugin.LogVerbose(
                $"Created {tier.PortalPrefab} ({string.Join(", ", tier.Recipe.Select(r => $"{r.Item} x{r.Amount}").ToArray())}).");

            return true;
        }

        /// <summary>
        /// Recolours the portal's emission. Vanilla portal_wood is black until it pairs and
        /// then blazes yellow at HDR intensity 5; here the idle state carries a dim biome
        /// tint instead, so six portals in one base are still tellable apart unpaired.
        /// </summary>
        private static void Tint(GameObject prefab, TierInfo tier)
        {
            var teleport = prefab.GetComponent<TeleportWorld>();
            if (teleport == null)
            {
                TollboundPlugin.LogWarning(
                    $"{tier.PortalPrefab} has no TeleportWorld component; left untinted.");
                return;
            }

            teleport.m_colorUnconnected = Scale(tier.Hue, TollboundConfig.IdleGlowIntensity.Value);
            teleport.m_colorTargetfound = Scale(tier.Hue, TollboundConfig.ConnectedGlowIntensity.Value);
        }

        /// <summary>
        /// Multiplies a hue into HDR range. Unity scales alpha along with the colour
        /// channels, so alpha is restored afterwards or the material renders transparent.
        /// </summary>
        private static Color Scale(Color hue, float intensity)
        {
            var scaled = hue * intensity;
            scaled.a = 1f;
            return scaled;
        }

        private static void AddLocalization(TierInfo tier)
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation(tier.NameToken, tier.DisplayName);
            loc.AddTranslation(tier.NameToken + "_description", tier.Description);
        }
    }
}
