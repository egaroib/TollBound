# Changelog

## 0.7.0
- Each portal now renders its own build-menu icon in its biome colour, so the six are
  tellable apart without hovering. The icon colour is baked into the sprite rather than
  applied in the UI, leaving vanilla's affordable/unaffordable icon tinting intact.
- Configured toll items are checked against the item database on load. A name that does
  not exist falls back to the shipped default with a loud error, instead of silently
  making that biome's cargo impossible to move.
- Portal hover text now shows the expected loss alongside the rate, not just the
  percentage — the number actually worth deciding on.

## 0.6.0
- New Tolls/Scaling setting. Flat (default) keeps one toll per biome per crossing whatever
  the haul. PerLoad charges one toll for every LoadSize units of that biome's cargo, so
  moving a warehouse costs proportionally more and boats stay worth using for bulk.
- LoadSize is per biome and defaults to 30, Valheim's own ore stack size, so the rule reads
  as one toll per stack. Rounds up: a single unit over a stack is a second load.
- Hover text and the ledger now price tolls through the gate's own calculation, so a quoted
  price cannot drift from the charged one, and both show the load count when above one.

## 0.5.1
- Surtling cores now scale across the ladder: 2, 3, 4, 6, 8, 10. They are already vanilla's
  portal ingredient, so a bigger portal wanting more core reads better than swapping in a
  different core at the top.
- Stations simplified to Workbench for the first three rungs and Forge for the last three.
  The Artisan table and Black forge read oddly in sequence for what is the same structure
  six times, and a Workbench is far easier to plant in the field than a Forge.
- Item report gained a candidate group for late-game recipe materials.

## 0.5.0
- The spirits speak. A crossing that is refused or paid for now draws a centre-screen line
  composed from two pools: an atmospheric opener and a demand, each a complete sentence, so
  six of each gives thirty-six combinations. Tone shifts once a biome's boss is dead.
- Lines live in BepInEx/config/Tollbound/voice.txt, written on first run and never
  overwritten. Add or edit lines without reinstalling; a new [mountain.restless.opener]
  section starts being used immediately.
- Black Forest and Swamp are fully voiced. Every other spirit uses the generic [fallback]
  pool until authored.
- New Appearance/SpiritDialogue setting turns the centre lines off, leaving only the
  itemised ledger.

## 0.4.1
- Loss rolls now log every lot under VerboseLogging, including skipped ones, so that
  "rolled and came up empty" can be told apart from "never rolled". At these rates a
  small haul losing nothing is the single most likely outcome, which makes a working
  mechanic look broken.

## 0.4.0
- Portal hover text now names the crossing: this portal's tier, the tier on the far end,
  and what the pair actually carries. Adds the toll due, what a living boss puts at risk,
  and what is blocking the crossing when something is.
- The inventory no-teleport slash now reflects the nearby portal instead of the vanilla
  all-or-nothing flag: it clears from cargo the crossing accepts and stays on the rest.
- Cargo the game restricts but Tollbound has no tier for is now refused rather than
  carried free. A game update adding a new ore fails closed instead of opening a hole.

## 0.3.0
- Portals of different tiers now connect freely. A crossing carries the LOWER of its two
  ends, so a Swamp portal linked to a Black Forest one is a Black Forest crossing both
  ways, and either linked to a vanilla portal carries nothing restricted at all.
- Removed the same-tier pairing restriction that made connections exclusive.
- The toll gate: ceiling check, per-biome tolls charged on the cargo you carry, and
  per-unit loss rolls for biomes whose boss still lives. Nothing is spent unless every
  check passes.
- The proximity swirl now reflects Tollbound's rules instead of vanilla's all-or-nothing
  check, and is tinted to match its portal.
- Biome portals deliberately do NOT set m_allowAllItems. Other portal mods read that flag
  as "no restrictions" and skip their own safeguards, which would have made these portals
  more permissive than vanilla rather than less.

## 0.2.2
- Fix biome portals never connecting. Game.Awake builds PortalPrefabHash from a
  serialized prefab list, and ZDOMan gates every portal path on it, so a portal whose
  hash is absent is never added to m_portalObjects and ConnectPortals never sees it.
  Tollbound's prefabs now register themselves as portals.
- Raise the default unconnected glow from 0.6 to 2.0, which is above the bloom
  threshold. Existing configs keep their own value; edit IdleGlowIntensity to change it.

## 0.2.1
- Fix the plugin failing to load at all: TierInfo.Recipe used a C# value tuple, which
  compiles against the net462 reference assemblies but has no System.ValueTuple at
  runtime under Valheim's Mono. The type initializer threw during Awake, before any
  pieces or patches were registered, so no portals appeared in the build menu.

## 0.2.0
- Six biome portal pieces, cloned from the vanilla wood portal and tinted per biome.
- Cargo tier model covering all 26 non-teleportable items, with per-tier toll and loss
  settings bound as admin-synced config.
- Same-tier portal pairing: a biome portal will not connect to a portal of another kind.
  Vanilla wood and stone portals still pair with each other as they always have.
- Item report now records Tollbound's own tier and loss classification per item.

## 0.1.0
- Initial release.
