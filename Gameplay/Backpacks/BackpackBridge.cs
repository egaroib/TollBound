using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tollbound.Model;

namespace Tollbound.Gameplay.Backpacks
{
    /// <summary>
    /// Reaches into backpack mods, so ore stashed in a pack is subject to the same rules as
    /// ore in your hands.
    ///
    /// Without this the mod has a hole straight through the middle of it: Tollbound reads
    /// the player's own inventory, and a backpack holds its contents in a nested Inventory
    /// that walk never touches. Everything would cross free.
    ///
    /// Bound entirely by reflection. Both mods ship API assemblies meant for exactly this,
    /// but referencing them directly would mean their next version bump breaks Tollbound for
    /// every user of it. Nothing here throws into the caller: a backpack mod that changed
    /// shape degrades to "no backpacks found" and is reported once in the log.
    /// </summary>
    internal static class BackpackBridge
    {
        internal const string AdventureBackpacksGuid = "vapok.mods.adventurebackpacks";
        internal const string BlaxxunBackpacksGuid = "org.bepinex.plugins.backpacks";

        private static bool _bound;

        // Adventure Backpacks: hands back real Inventory objects, so its contents can be
        // walked exactly like the player's own.
        private static MethodInfo _abIsBackpack;
        private static MethodInfo _abGetBackpack;
        private static MethodInfo _abGetEquipped;
        private static FieldInfo _abInventoryField;

        // Backpacks (blaxxun): no way to obtain the nested Inventory without reaching into
        // ItemDataManager types it renames at build time, so its own count/delete API is
        // used instead. That API matches on the localisation token rather than the prefab
        // name, hence TokenFor below.
        private static MethodInfo _bxCount;
        private static MethodInfo _bxDelete;

        private static readonly Dictionary<string, string> TokenCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static bool AdventureBackpacksLoaded => _abGetEquipped != null;

        internal static bool BlaxxunBackpacksLoaded => _bxCount != null;

        internal static bool AnyLoaded => AdventureBackpacksLoaded || BlaxxunBackpacksLoaded;

        internal static void Bind()
        {
            if (_bound)
            {
                return;
            }

            _bound = true;

            BindAdventureBackpacks();
            BindBlaxxunBackpacks();

            if (!AnyLoaded)
            {
                TollboundPlugin.LogInfo("No backpack mod detected.");
                return;
            }

            var found = new List<string>();
            if (AdventureBackpacksLoaded)
            {
                found.Add("AdventureBackpacks");
            }

            if (BlaxxunBackpacksLoaded)
            {
                found.Add("Backpacks");
            }

            TollboundPlugin.LogInfo(
                $"Backpack support active for: {string.Join(", ", found.ToArray())}. " +
                "Cargo in a pack is tolled and taxed like anything else.");
        }

        private static void BindAdventureBackpacks()
        {
            var api = FindType("AdventureBackpacks.API.ABAPI");
            if (api == null)
            {
                return;
            }

            try
            {
                _abIsBackpack = api.GetMethod("IsBackpack", new[] { typeof(ItemDrop.ItemData) });
                _abGetBackpack = api.GetMethod("GetBackpack", new[] { typeof(ItemDrop.ItemData) });
                _abGetEquipped = api.GetMethod("GetEquippedBackpack", new[] { typeof(Player) });

                if (_abIsBackpack == null || _abGetBackpack == null || _abGetEquipped == null)
                {
                    Unbind("AdventureBackpacks", "expected API methods were not found");
                }
            }
            catch (Exception e)
            {
                Unbind("AdventureBackpacks", e.Message);
            }
        }

        private static void BindBlaxxunBackpacks()
        {
            var api = FindType("Backpacks.API");
            if (api == null)
            {
                return;
            }

            try
            {
                _bxCount = api.GetMethod("CountItemsInBackpacks",
                    new[] { typeof(Inventory), typeof(string), typeof(bool) });
                _bxDelete = api.GetMethod("DeleteItemsFromBackpacks",
                    new[] { typeof(Inventory), typeof(string), typeof(int) });

                if (_bxCount == null || _bxDelete == null)
                {
                    Unbind("Backpacks", "expected API methods were not found");
                }
            }
            catch (Exception e)
            {
                Unbind("Backpacks", e.Message);
            }
        }

        private static void Unbind(string which, string why)
        {
            TollboundPlugin.LogWarning(
                $"{which} is installed but its API did not look as expected ({why}). " +
                "Cargo inside its backpacks will NOT be tolled. Report this, it is a " +
                "compatibility break rather than a config problem.");

            if (which == "AdventureBackpacks")
            {
                _abIsBackpack = _abGetBackpack = _abGetEquipped = null;
            }
            else
            {
                _bxCount = _bxDelete = null;
            }
        }

        /// <summary>
        /// Every backpack Inventory the player is carrying, for mods that expose them.
        /// Deduplicated: an equipped backpack also sits in the player's inventory.
        /// </summary>
        private static List<Inventory> OpenInventories(Player player)
        {
            var found = new List<Inventory>();

            if (!AdventureBackpacksLoaded || player == null)
            {
                return found;
            }

            try
            {
                Add(found, InventoryOf(_abGetEquipped.Invoke(null, new object[] { player })));

                foreach (var item in player.GetInventory().GetAllItems())
                {
                    if (item == null
                        || !(bool)_abIsBackpack.Invoke(null, new object[] { item }))
                    {
                        continue;
                    }

                    Add(found, InventoryOf(_abGetBackpack.Invoke(null, new object[] { item })));
                }
            }
            catch (Exception e)
            {
                TollboundPlugin.LogVerbose($"AdventureBackpacks read failed: {e.Message}");
            }

            return found;
        }

        private static void Add(List<Inventory> list, Inventory inventory)
        {
            if (inventory != null && !list.Contains(inventory))
            {
                list.Add(inventory);
            }
        }

        /// <summary>
        /// Reads the Inventory out of an ABAPI.Backpack. The API returns a nullable struct,
        /// so a boxed null simply means no backpack.
        /// </summary>
        private static Inventory InventoryOf(object backpack)
        {
            if (backpack == null)
            {
                return null;
            }

            if (_abInventoryField == null)
            {
                _abInventoryField = backpack.GetType().GetField("Inventory");
                if (_abInventoryField == null)
                {
                    return null;
                }
            }

            return _abInventoryField.GetValue(backpack) as Inventory;
        }

        /// <summary>
        /// Reports every piece of restricted cargo held in backpacks, as (prefab, count).
        ///
        /// Mods exposing an Inventory are walked directly, which also catches items
        /// Tollbound has no tier for. The token-API path can only ask about prefabs it
        /// already knows, so an unrecognised item hidden in one of those packs is invisible
        /// — a limitation of that API, noted in the README.
        /// </summary>
        internal static void CollectCargo(Player player, Action<string, int> onCargo)
        {
            if (!AnyLoaded || player == null)
            {
                return;
            }

            foreach (var inventory in OpenInventories(player))
            {
                foreach (var item in inventory.GetAllItems())
                {
                    var prefab = PortalIdentity.PrefabNameOf(item);
                    if (prefab != null)
                    {
                        onCargo(prefab, item.m_stack);
                    }
                }
            }

            if (!BlaxxunBackpacksLoaded)
            {
                return;
            }

            foreach (var prefab in CargoRegistry.AllCargo)
            {
                var held = CountViaToken(player, prefab);
                if (held > 0)
                {
                    onCargo(prefab, held);
                }
            }
        }

        /// <summary>Units of one prefab held across every backpack the player carries.</summary>
        internal static int Count(Player player, string prefabName)
        {
            if (!AnyLoaded || player == null || string.IsNullOrEmpty(prefabName))
            {
                return 0;
            }

            var total = OpenInventories(player)
                .Sum(inv => inv.GetAllItems()
                    .Where(i => PortalIdentity.PrefabNameOf(i) == prefabName)
                    .Sum(i => i.m_stack));

            return total + CountViaToken(player, prefabName);
        }

        /// <summary>
        /// Removes up to <paramref name="amount"/> units from backpacks and returns how many
        /// were actually taken, so a pack that refuses a removal cannot be reported as a loss.
        /// </summary>
        internal static int Remove(Player player, string prefabName, int amount)
        {
            if (!AnyLoaded || player == null || amount <= 0)
            {
                return 0;
            }

            var taken = 0;

            foreach (var inventory in OpenInventories(player))
            {
                foreach (var item in inventory.GetAllItems().ToList())
                {
                    if (taken >= amount)
                    {
                        break;
                    }

                    if (PortalIdentity.PrefabNameOf(item) != prefabName)
                    {
                        continue;
                    }

                    var take = Math.Min(item.m_stack, amount - taken);
                    inventory.RemoveItem(item, take);
                    taken += take;
                }
            }

            if (taken < amount)
            {
                taken += RemoveViaToken(player, prefabName, amount - taken);
            }

            return taken;
        }

        private static int CountViaToken(Player player, string prefabName)
        {
            if (!BlaxxunBackpacksLoaded)
            {
                return 0;
            }

            var token = TokenFor(prefabName);
            if (token == null)
            {
                return 0;
            }

            try
            {
                // onlyRemoveable: false. Anything being carried is subject to the rules,
                // whether or not the pack would let you take it back out.
                return (int)_bxCount.Invoke(null,
                    new object[] { player.GetInventory(), token, false });
            }
            catch (Exception e)
            {
                TollboundPlugin.LogVerbose($"Backpacks count failed: {e.Message}");
                return 0;
            }
        }

        private static int RemoveViaToken(Player player, string prefabName, int amount)
        {
            if (!BlaxxunBackpacksLoaded || amount <= 0)
            {
                return 0;
            }

            var token = TokenFor(prefabName);
            if (token == null)
            {
                return 0;
            }

            try
            {
                var before = CountViaToken(player, prefabName);
                _bxDelete.Invoke(null, new object[] { player.GetInventory(), token, amount });

                // Measured rather than assumed: the API declines the whole operation when
                // it cannot satisfy the count, and reporting a loss that did not happen
                // would tell the player they lost ore they still have.
                return Math.Max(0, before - CountViaToken(player, prefabName));
            }
            catch (Exception e)
            {
                TollboundPlugin.LogVerbose($"Backpacks delete failed: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// The localisation token behind a prefab, which is what the Backpacks API matches
        /// on. Cached: this is called for every known cargo type on every scan.
        /// </summary>
        private static string TokenFor(string prefabName)
        {
            if (TokenCache.TryGetValue(prefabName, out var cached))
            {
                return cached;
            }

            string token = null;

            if (ObjectDB.instance != null
                && ObjectDB.instance.TryGetItemPrefab(prefabName, out var go)
                && go != null)
            {
                var drop = go.GetComponent<ItemDrop>();
                token = drop?.m_itemData?.m_shared?.m_name;
            }

            TokenCache[prefabName] = token;
            return token;
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
