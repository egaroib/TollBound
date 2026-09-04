# Changelog

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
