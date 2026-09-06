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
            ApplyIcon(prefab, tier);

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

            TintModel(teleport, tier);
            TintSwirl(teleport, tier);
        }

        /// <summary>
        /// Tints the frame's own material.
        ///
        /// TeleportWorld writes the emission colour every frame in Update, so this is not
        /// needed for the live portal — but a rendered icon captures the prefab as it sits,
        /// before any Update has run, and would otherwise come out vanilla yellow.
        ///
        /// Touching .material instantiates a copy owned by this renderer, so the vanilla
        /// portal's shared material is not affected.
        /// </summary>
        private static void TintModel(TeleportWorld teleport, TierInfo tier)
        {
            if (teleport.m_model == null)
            {
                return;
            }

            var material = teleport.m_model.material;
            if (material == null)
            {
                return;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",
                Scale(tier.Hue, TollboundConfig.ConnectedGlowIntensity.Value));
        }

        /// <summary>
        /// Renders the tinted prefab into its own build-menu icon.
        ///
        /// Six portals cloned from one prefab share one icon, so the hammer menu shows six
        /// identical entries. Tinting the icon's colour in the UI is not an option: vanilla
        /// writes m_icon.color itself to signal whether a piece is affordable, and
        /// overwriting that would cost the player a more useful cue than it bought them.
        /// Baking the colour into the sprite leaves that mechanism alone.
        /// </summary>
        private static void ApplyIcon(GameObject prefab, TierInfo tier)
        {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                return;
            }

            // A dedicated server has no graphics device; Render returns a blank sprite
            // there, and nothing would ever draw it anyway.
            if (GUIManager.IsHeadless())
            {
                return;
            }

            try
            {
                var sprite = RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab)
                {
                    Rotation = RenderManager.IsometricRotation,
                    UseCache = true,
                    TargetPlugin = TollboundPlugin.Instance.Info.Metadata,

                    // The portal's own particle effects would otherwise fill the frame.
                    ParticleSimulationTime = -1f,
                });

                if (sprite != null)
                {
                    piece.m_icon = sprite;
                    TollboundPlugin.LogVerbose($"{tier.PortalPrefab}: rendered its own icon.");
                }
                else
                {
                    TollboundPlugin.LogWarning(
                        $"{tier.PortalPrefab}: icon render returned nothing, keeping the " +
                        "vanilla portal icon.");
                }
            }
            catch (System.Exception e)
            {
                // An icon is cosmetic. Never let it stop the piece registering.
                TollboundPlugin.LogWarning($"{tier.PortalPrefab}: icon render failed ({e.Message}).");
            }
        }

        /// <summary>
        /// Recolours the swirl that appears when you stand at a connected portal.
        ///
        /// EffectFade holds it as particle systems plus a light, both of which live inside
        /// this cloned prefab and so are safe to modify. The m_connected effect list is
        /// deliberately left alone: those are shared prefabs, and tinting one would recolour
        /// every vanilla portal in the world too.
        /// </summary>
        private static void TintSwirl(TeleportWorld teleport, TierInfo tier)
        {
            if (teleport.m_target_found == null)
            {
                return;
            }

            var root = teleport.m_target_found.gameObject;
            var tint = tier.Hue;
            tint.a = 1f;

            var systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (var system in systems)
            {
                // startColor multiplies the particle's texture, so a hue at full alpha
                // tints without washing the effect out.
                var main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
            }

            var lights = root.GetComponentsInChildren<Light>(includeInactive: true);
            foreach (var light in lights)
            {
                light.color = tint;
            }

            TollboundPlugin.LogVerbose(
                $"{tier.PortalPrefab}: tinted {systems.Length} particle system(s) " +
                $"and {lights.Length} light(s).");
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
