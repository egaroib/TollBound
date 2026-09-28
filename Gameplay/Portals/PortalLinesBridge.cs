using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Tollbound.Config;
using Tollbound.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Tollbound.Gameplay.Portals
{
    /// <summary>
    /// Colours PortalLines' map pins by Tollbound tier.
    ///
    /// PortalLines keeps every pin it adds in a private static map of pin to portal record,
    /// and that record carries the portal's prefab name — including for portals it only
    /// remembers from disk. It tints pins in its own UpdatePins postfix, starting from the
    /// white vanilla gives every unowned pin: red for a portal with no partner, orange for a
    /// tag shared by three or more, and faded for remembered ones.
    ///
    /// This runs after that. A pin PortalLines left white takes the tier's hue at the alpha
    /// PortalLines chose. A pin it tinted keeps that warning colour, because "this goes
    /// nowhere" is the more urgent thing to see, and gets a dot in the tier's hue on its
    /// corner so it still reads as a Tollbound portal of a particular biome.
    ///
    /// Everything is found by reflection. Without PortalLines, or with a version that has
    /// renamed what this reads, the postfix returns on its first line and nothing changes.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdatePins")]
    [HarmonyAfter(Guid)]
    [HarmonyPriority(Priority.Last)]
    internal static class PortalLinesBridge
    {
        internal const string Guid = "com.jumpingmushroom.portallines";

        private const string BadgeName = "TollboundTierBadge";

        private static bool _bound;
        private static IDictionary _owned;
        private static FieldInfo _prefabName;
        private static Sprite _dot;

        internal static bool Loaded { get; private set; }

        internal static void Bind()
        {
            if (_bound)
            {
                return;
            }

            _bound = true;

            var pins = FindType("PortalLines.UI.PortalPins");
            if (pins == null)
            {
                return;
            }

            var owned = AccessTools.Field(pins, "s_owned");
            var entry = FindType("PortalLines.Model.PortalEntry");
            var prefabName = entry == null ? null : AccessTools.Field(entry, "PrefabName");

            if (owned == null || !owned.IsStatic || prefabName == null
                || !(owned.GetValue(null) is IDictionary map))
            {
                TollboundPlugin.LogWarning(
                    "PortalLines is installed but its pin list was not where expected. " +
                    "Its map pins will keep their own colours.");
                return;
            }

            _owned = map;
            _prefabName = prefabName;
            Loaded = true;
            TollboundPlugin.LogInfo("PortalLines detected. Its map pins now show biome portal tiers.");
        }

        private static void Postfix()
        {
            if (!Loaded || _owned.Count == 0 || !TollboundConfig.PortalLinesPins.Value)
            {
                return;
            }

            try
            {
                foreach (DictionaryEntry item in _owned)
                {
                    Paint(item.Key as Minimap.PinData, item.Value);
                }
            }
            catch (Exception e)
            {
                // Once, not every frame: stop rather than flood the log from a map redraw.
                Loaded = false;
                TollboundPlugin.LogWarning(
                    $"Stopped tinting PortalLines pins ({e.Message}). They keep their own colours.");
            }
        }

        private static void Paint(Minimap.PinData pin, object entry)
        {
            var icon = pin?.m_iconElement;
            if (icon == null || entry == null)
            {
                return;
            }

            var tier = Tiers.Get(Tiers.TierOfPortal(_prefabName.GetValue(entry) as string));
            if (tier == null)
            {
                // Wood, stone, or another mod's portal. Leave whatever badge a reused pin
                // might carry switched off.
                ShowBadge(pin, null, 0f);
                return;
            }

            var current = icon.color;
            var hue = new Color(tier.Hue.r, tier.Hue.g, tier.Hue.b, current.a);

            if (IsUntinted(current))
            {
                icon.color = hue;
                ShowBadge(pin, null, 0f);
            }
            else
            {
                ShowBadge(pin, hue, current.a);
            }
        }

        // Vanilla draws every unowned pin white; any other colour is PortalLines speaking.
        private static bool IsUntinted(Color c) => c.r > 0.99f && c.g > 0.99f && c.b > 0.99f;

        private static void ShowBadge(Minimap.PinData pin, Color? hue, float alpha)
        {
            var root = pin.m_uiElement;
            if (root == null)
            {
                return;
            }

            var existing = root.Find(BadgeName);
            if (hue == null)
            {
                if (existing != null && existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            // The badge is a child of the pin's own marker, so the game destroys it with
            // the marker whenever the pin scrolls off the map or the map changes mode.
            var badge = existing != null ? existing.GetComponent<Image>() : CreateBadge(root);
            if (!badge.gameObject.activeSelf)
            {
                badge.gameObject.SetActive(true);
            }

            var c = hue.Value;
            c.a = alpha;
            badge.color = c;
        }

        private static Image CreateBadge(RectTransform root)
        {
            var go = new GameObject(BadgeName, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);

            // Anchored to the lower-right quarter of the pin, so it scales with the pin on
            // both the large map and the minimap.
            rt.anchorMin = new Vector2(0.55f, -0.05f);
            rt.anchorMax = new Vector2(1.05f, 0.45f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = Dot();
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// A filled disc with a dark rim. The rim survives the tint, since tinting
        /// multiplies, which keeps a pale hue legible over snow.
        /// </summary>
        private static Sprite Dot()
        {
            if (_dot != null)
            {
                return _dot;
            }

            const int size = 32;
            const float radius = size / 2f - 1f;
            const float rim = 3f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var centre = (size - 1) / 2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
                    var shade = d > radius - rim ? 0.1f : 1f;
                    var a = Mathf.Clamp01(radius + 0.5f - d);
                    tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                }
            }

            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _dot;
        }

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
