# Tollbound

Portals you earn one biome at a time. What you carry decides what you pay, and which
bosses you have put down decides how much of it survives the trip.

Six new portal pieces, each built from its own biome's materials. Each is a **licence
with a ceiling**: a Swamp portal carries Swamp-tier cargo and everything below it, and
refuses anything above. A Swamp portal only ever connects to another Swamp portal, so
reaching a new biome means building there *and* building the matching end at home.

The toll follows the **cargo, not the ground you are standing on**. Hauling bronze and
iron out of your base costs greydwarf eyes *and* entrails, exactly as it would leaving
the Swamp.

Kill the boss and that biome's cargo travels intact. Leave it alive and the spirit takes
a cut of the metal. Ores and bars can be lost; dragon eggs, extractors, cogwheels and
Hildir's chests never are.

**Wood and stone portals behave exactly as vanilla.** The stone portal already carries
everything, so earning it in the base game is the natural graduation — Tollbound's whole
ladder sits before it.

## Who needs to install this

**Every player and the server.** This changes game rules. The server owns portal pairing
and piece registration; each client owns its own teleport check. A client without the mod
is refused at the version handshake rather than allowed in to desync quietly.

## The ladder

| Portal | Cargo it adds | Toll per trip | Loss while the boss lives |
|---|---|---|---|
| Black Forest | Copper, tin, bronze, ores and scrap | 5 × Greydwarf eye | 8% |
| Swamp | Iron, iron ore, scrap iron | 6 × Entrails | 11% |
| Mountain | Silver, silver ore, dragon eggs | 4 × Freeze gland | 14% |
| Plains | Black metal, black metal scrap | 5 × Needle | 17% |
| Mistlands | Dvergr extractors, Hildir's chests | 4 × Bilebag | — |
| Ashlands | Flametal, cogwheels, springs, iron pits | 5 × Charred bone | 22% |

Tolls stack across every biome represented in your pack, not just the portal's own tier.
Losses are rolled once per unit, so a large haul loses roughly the listed share.

By default a toll covers a crossing regardless of how much you carry. Servers wanting more
friction can switch `Tolls/Scaling` to `PerLoad`, which charges a toll for every stack.

## The spirits

Each biome's spirit speaks when a crossing is refused or paid for, in its own register —
and changes tone once you have killed it. Lines live in
`BepInEx/config/Tollbound/voice.txt` and can be edited or added to without reinstalling.
The file is never rewritten, so your edits survive updates.

## Requirements

- [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## Installation

Install with a mod manager, or drop `Tollbound.dll` into `BepInEx/plugins/`.

## Configuration

Settings live in `BepInEx/config/com.ragemedia.tollbound.cfg`, generated on first launch.

| Section | Setting | Synced | What it does |
|---|---|---|---|
| General | `VerboseLogging` | no | Detailed per-event logging. Off by default. |
| Diagnostics | `WriteItemReport` | no | Writes a survey of every non-teleportable item in your install to `BepInEx/config/Tollbound/item-report.md`. Useful when other mods add items Tollbound has no tier for. |
| Appearance | `IdleGlowIntensity` | no | Brightness of a portal's glow before it pairs. Vanilla portals sit dark; the default keeps each biome tellable apart by colour. |
| Appearance | `ConnectedGlowIntensity` | no | Brightness once connected. Vanilla uses 5. |
| Tolls | `Scaling` | **admin** | `Flat` charges one toll per biome per crossing. `PerLoad` charges one toll for every `LoadSize` units, so bulk moves cost proportionally more. |
| Tier – *biome* | `LoadSize` | **admin** | Units covered by one toll under `PerLoad`. Defaults to 30 — one ore stack. |
| Tier – *biome* | `TollItem` | **admin** | Prefab name of the item that biome's spirit takes as passage. |
| Tier – *biome* | `TollAmount` | **admin** | How many, per crossing. 0 makes that biome toll-free. |
| Tier – *biome* | `LossRate` | **admin** | Per-unit chance that biome's ore or bars are destroyed while its boss lives. 0 disables it. |

Settings marked **admin** are server-controlled: a client cannot lower its own loss rate
or zero out a toll. Appearance settings are local and change nothing but your own screen.

## Compatibility

Backpack mods store items in a nested inventory that vanilla's teleport check never walks.
Tollbound reads backpack contents directly rather than trusting a mod's teleportable flag,
so ore cannot be smuggled through in a pack. Support is planned for
[AdventureBackpacks](https://valheim.thunderstore.io/package/Vapok/AdventureBackpacks/) and
[Backpacks](https://valheim.thunderstore.io/package/Smoothbrain/Backpacks/) and is not yet
implemented.

## Licence

MIT.
