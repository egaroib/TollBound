# Tollbound

Portals you earn one biome at a time. What you carry decides what you pay, and which
bosses you have put down decides how much of it survives the trip.

Vanilla's portal rule is all or nothing: ore never goes through, and then one day the stone
portal arrives and everything does. Tollbound fills that gap with a ladder — six new portal
pieces, each built from its own biome's materials, each carrying a little more than the last.

**Wood and stone portals behave exactly as vanilla.** The stone portal already carries
everything, so earning it in the base game is still the graduation. Tollbound's whole ladder
sits before it.

![Six Tollbound portals in a row, each glowing in its own biome's colour](https://raw.githubusercontent.com/egaroib/TollBound/main/img/tollbound_portals.jpg)

*The whole ladder, built out — Black Forest to Ashlands, left to right.*

## How it works

**Each portal is a licence with a ceiling.** A Swamp portal carries Swamp-tier cargo and
everything below it, and refuses anything above.

**Portals of different tiers connect freely, and a crossing carries the lower of its two
ends.** A Swamp portal linked to a Black Forest one is a Black Forest crossing in both
directions. Link either to a plain wood portal and nothing restricted goes through at all.

**The toll follows the cargo, not the ground you are standing on.** Every restricted item
belongs to a biome, and carrying it pays that biome's spirit wherever the trip starts.
Hauling bronze and iron out of your base costs greydwarf eyes *and* entrails, exactly as it
would leaving the Swamp.

**Kill the boss and that biome's cargo travels intact.** Leave it alive and the spirit takes
a cut of the metal on the way through. Ores and bars can be lost; dragon eggs, extractors,
cogwheels and Hildir's chests never are.

Nothing is spent unless the whole crossing is allowed. A refused trip costs you nothing.

## The ladder

| Portal | Cargo it adds | Toll per crossing | Loss while the boss lives |
|---|---|---|---|
| Black Forest | Copper, tin, bronze, their ores and scrap | 5 × Greydwarf eye | 8% |
| Swamp | Iron, iron ore, scrap iron | 6 × Entrails | 11% |
| Mountain | Silver, silver ore, dragon eggs | 4 × Freeze gland | 14% |
| Plains | Black metal, black metal scrap | 5 × Needle | 17% |
| Mistlands | Dvergr extractors, Hildir's chests | 4 × Bilebag | — |
| Ashlands | Flametal, cogwheels, springs, iron pits | 5 × Charred bone | 22% |

Tolls stack across every biome in your pack, not just the portal's own tier. Losses are
rolled once per unit, so a big haul loses roughly the listed share and a small one is luck.

By default one toll covers a crossing however much you carry. Servers wanting more friction
can switch `Tolls/Scaling` to `PerLoad`, which charges a toll for every stack.

## Building the portals

Each portal costs **its own biome's metal**. That means the first haul out of every biome is
still a boat trip — you cannot build the thing that carries iron until you have brought iron
home the hard way. From the second run onward the biome is open.

| Portal | Cost | Station |
|---|---|---|
| Black Forest | Fine wood ×20, Greydwarf eye ×10, Surtling core ×2, Copper ×5 | Workbench |
| Swamp | Fine wood ×20, Ancient bark ×10, Surtling core ×3, Iron ×5 | Workbench |
| Mountain | Fine wood ×20, Obsidian ×10, Surtling core ×4, Silver ×5 | Workbench |
| Plains | Fine wood ×20, Needle ×10, Surtling core ×6, Black metal ×5 | Forge |
| Mistlands | Yggdrasil wood ×20, Black marble ×10, Surtling core ×8, Refined eitr ×2 | Forge |
| Ashlands | Ashwood ×20, Grausten ×10, Surtling core ×10, Flametal ×5 | Forge |

Each portal glows in its biome's colour and carries its own icon in the build menu, so six
of them in one base are still tellable apart.

![A Tollbound portal firing as a player carrying a backpack steps through](https://raw.githubusercontent.com/egaroib/TollBound/main/img/tollbound_vfx.jpg)

*A paid crossing. Cargo in a backpack is weighed like anything else.*

## Knowing where you stand

Look at a portal and it tells you what this particular crossing will carry, what the toll
comes to, and what a living boss is putting at risk:

![Portal hover text reading: Mountain to Plains. Carries Mountain cargo and below. Toll: Greydwarf Eye x5. At risk: 22 at 8% to the Elder](https://raw.githubusercontent.com/egaroib/TollBound/main/img/tollbound_hover.jpg)

The no-teleport slash in your inventory tells the truth too. Stand at a Swamp portal and it
clears from your iron while staying on your flametal.

## The spirits

Each biome's spirit speaks when a crossing is refused or paid for, in its own register — and
changes tone once you have killed it.

> The bog exhales. Nothing leaves the marsh unweighed.

> The mud parts where you walk. The mire outlived him. So did the toll.

![A refused crossing: the Elder demands Greydwarf Eye x5, and a spirit line reads "Something old shifts beneath the moss. Nothing leaves these woods unweighed."](https://raw.githubusercontent.com/egaroib/TollBound/main/img/tollbound_message.jpg)

*A refusal: the itemised ledger top-left, the spirit's line centre-screen. Nothing is spent.*

Lines live in `BepInEx/config/Tollbound/voice.txt` and can be edited or added to without
reinstalling. The file is never rewritten, so your edits survive updates; sections added by
an update are appended without touching anything you wrote. Set `Appearance/SpiritDialogue`
to `false` for the itemised ledger alone.

## Who needs to install this

**Every player and the server.** This changes game rules. The server registers the pieces and
is what recognises them as portals at all; each client decides its own crossings. A client
without the mod is refused at the version handshake rather than allowed in to desync quietly.

Gameplay settings are admin-synced from the server, so a client cannot lower its own loss
rate or zero out a toll. Appearance settings are local.

## Requirements

- [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## Installation

Install with a mod manager, or drop `Tollbound.dll` into `BepInEx/plugins/`.

## Compatibility

### Backpacks

Backpack mods hold their contents in a nested inventory that the game's own teleport check
never walks. Tollbound reads them directly, so cargo in a pack counts toward the crossing's
ceiling, is priced into the toll, and can be taken by a loss roll — stashing ore in a
backpack does not get it past a spirit. Tolls come from your own inventory first.

Supported: [AdventureBackpacks](https://valheim.thunderstore.io/package/Vapok/AdventureBackpacks/)
and [Backpacks](https://valheim.thunderstore.io/package/Smoothbrain/Backpacks/).

### Portal mods

[TargetPortal](https://valheim.thunderstore.io/package/Smoothbrain/TargetPortal/) is
supported. Warping from a biome portal to a map pin is gated exactly like walking through
one — same ceiling from both ends, same tolls, same loss rolls. Leaving a wood or stone
portal by map is left alone.

### Everything else

Both of the above are soft dependencies bound by reflection, so neither is required and
their updates cannot break this mod. If one is installed but its API has changed shape,
Tollbound says so in the log rather than quietly letting cargo through.

Tollbound portals deliberately do not set `m_allowAllItems`. Portal mods read that flag as
"this portal has no restrictions" and skip their own safeguards, so setting it would make a
biome portal *more* permissive than a vanilla one. Left unset, a portal mod Tollbound does
not yet know about falls back to its own restrictions — stricter than intended, never a way
to smuggle cargo.

Items another mod marks non-teleportable that Tollbound has no tier for are refused rather
than carried free, so a mod or game update adding a new ore fails closed. Turn on
`Diagnostics/WriteItemReport` and read `BepInEx/config/Tollbound/item-report.md` to see
exactly what your install contains and how Tollbound classifies it.

One limit worth stating: backpack contents are readable only on the owning client, so a
server cannot validate them.

## Configuration

Settings live in `BepInEx/config/com.ragemedia.tollbound.cfg`, generated on first launch.

| Section | Setting | Synced | What it does |
|---|---|---|---|
| General | `VerboseLogging` | no | Detailed per-event logging, including why each crossing was allowed or refused. Off by default. |
| Diagnostics | `WriteItemReport` | no | Writes a survey of every non-teleportable item in your install, and how Tollbound tiers it. |
| Appearance | `SpiritDialogue` | no | The spirits' centre-screen lines. Off leaves only the itemised ledger. |
| Appearance | `IdleGlowIntensity` | no | Brightness of a portal's glow before it pairs. Vanilla portals sit dark; the default keeps each biome tellable apart. |
| Appearance | `ConnectedGlowIntensity` | no | Brightness once connected. Vanilla uses 5. |
| Tolls | `Scaling` | **admin** | `Flat` charges one toll per biome per crossing. `PerLoad` charges one toll for every `LoadSize` units, so bulk moves cost proportionally more. |
| Tier – *biome* | `TollItem` | **admin** | Prefab name of the item that biome's spirit takes as passage. A name that does not exist falls back to the default and says so in the log. |
| Tier – *biome* | `TollAmount` | **admin** | How many, per crossing. 0 makes that biome toll-free. |
| Tier – *biome* | `LoadSize` | **admin** | Units covered by one toll under `PerLoad`. Defaults to 30 — one ore stack. |
| Tier – *biome* | `LossRate` | **admin** | Per-unit chance that biome's ore or bars are destroyed while its boss lives. 0 disables it. |

Settings marked **admin** are server-controlled: a client cannot lower its own loss rate or
zero out a toll. Appearance settings are local and change nothing but your own screen.

## Source

[github.com/egaroib/TollBound](https://github.com/egaroib/TollBound)

## Licence

MIT.
