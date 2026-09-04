# Tollbound

A Valheim mod that replaces the vanilla all-or-nothing portal rule with a progression
you earn one biome at a time.

Six new portal pieces, each built from its own biome's materials. Each is a **licence
with a ceiling**: a Swamp portal carries Swamp-tier cargo and everything below it, and
refuses anything above. A Swamp portal only ever connects to another Swamp portal, so
reaching a new biome means building there *and* building the matching end at home.

The toll follows the **cargo, not the ground you are standing on**. Every restricted
item belongs to a biome, and carrying it charges that biome's spirit wherever the trip
starts — hauling bronze and iron out of your base costs greydwarf eyes *and* entrails,
exactly as it would leaving the Swamp.

Kill the boss and that biome's cargo travels intact. Leave it alive and the spirit takes
a cut of the metal. Ores and bars can be lost; quest and mechanism items never are.

Wood and stone portals behave exactly as vanilla. `portal_stone` already carries
everything, so earning it in the base game is the natural graduation — Tollbound's whole
ladder sits before it and never patches it.

## The ladder

| Portal | Cargo it adds | Toll | Loss while the boss lives |
|---|---|---|---|
| Black Forest | Copper, tin, bronze, and their ores and scrap | 5 × Greydwarf eye | 5% |
| Swamp | Iron, iron ore, scrap iron | 6 × Entrails | 7% |
| Mountain | Silver, silver ore, dragon eggs | 4 × Freeze gland | 9% |
| Plains | Black metal and black metal scrap | 5 × Needle | 11% |
| Mistlands | Dvergr extractors, Hildir's chests | 4 × Bilebag | — |
| Ashlands | Flametal, cogwheels, springs, iron pits | 5 × Charred bone | 15% |

Tolls stack across every biome represented in your pack, not just the portal's own tier.
Every number above is a config entry.

## Installing

**Every player and the server need this mod.** The server owns portal pairing and piece
registration; each client owns its own teleport check. A client without it is refused at
the version handshake rather than allowed in to desync quietly.

Gameplay settings are admin-synced from the server, so a client cannot lower its own loss
rate or zero out a toll. Appearance settings are local and not synced.

## Status

In development. The portal pieces, tier model and same-tier pairing are implemented;
tolls, loss rolls, spirit dialogue, backpack support and the inventory icon are not yet.

## Building

This project is developed inside a Valheim mod workspace that supplies `Directory.Build.props`
and a generated `Environment.props` pointing at a local Steam install, a Gale profile with
BepInEx and Jötunn, and a decompile of the game for reference. It is not currently
standalone-buildable from a bare clone — that will come if anyone wants to contribute.

Targets net462, references publicized game assemblies, and patches via HarmonyX. Content
is registered through Jötunn. No custom art or AssetBundles: the portals are the vanilla
`portal_wood` prefab cloned and re-tinted.

## Licence

MIT. See [LICENSE](LICENSE).
