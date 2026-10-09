# Turret classes, drone layouts and preview rotation

## Implemented

- All inventory cells, in all containers, now use **64×64 UI pixels**. There is no per-chassis
  cell scaling. The vehicle window fills the available screen minus 16px margins; its central
  canvas supports scrolling and panning. See [large-grid navigation](large-grid-navigation.md).
  The layout Rotate/Cancel buttons are removed; R, Esc and right-click still work.
  The separate right-hand preview rotation toggle remains.
- MDRN (Mini Drone) has a hover-drone body, integrated weapon silhouette and no large turret
  pedestal. Its configured carrier remains Integrated Mount.
- GDRN (Scout Drone) uses a small tracked silhouette. This is presentation only: its original
  wheeled locomotor, terrain/crushing behavior and combat values remain unchanged.
- A **Mini Turret Mount** with class **Mini** replaces GDRN's generic integrated-mount entry.
  Original GDRN art has no WithSpriteTurret and uses AttackFrontal. This is a socket/native
  binding definition, NOT proof that independently rotating/mixed mini turret art is supported.
- VULC has a distinct **Dual Gatling Turret** component, currently admitted on VULC only. The
  sprite includes both barrels. All four air and three ground weapon stages, spin-up conditions,
  cargo and original traits are inherited unchanged. Its old provisional allocation is retained.
- Hover MLRS's standard carrier is **Dual Missile Launcher**, class **Medium Turret**;
  its original ground/air and both upgraded channels remain intact.
- MARV's standard carrier is **Triple Ion Cannon**, class **Super Heavy**.
- TITN's carrier is **Heavy Walker Turret**, class **Heavy Walker**; its weapon label is
  **120mm Cannon and Missiles**. TitanGun and TitanTusk remain the original weapon channels.
  The caliber is a presentation label, not a newly calibrated weapon scalar.
- Titan has a vertical **4×6 chassis grid** and a larger **5×4 turret grid**. The engine and
  generator stack vertically, with 4×1 running gear along the bottom. These are provisional
  editor-space allocations, not runtime physical capacities or changes to mass/price.
- Hulls with declared allowed turret classes reject mismatched classes. The explicit native
  whitelist remains authoritative; matching class never admits an unreviewed assembly.
- Known legacy integrated-mount IDs migrate narrowly on GDRN/VULC only. Other profiles, user
  names and frozen maps are unchanged. Numeric stock calculation fixtures remain unchanged.
- Preview bodies and separately rendered turrets rotate in opposite directions on the same
  24-second clock. Sprite previews need absolute turret angles; voxel previews need relative
  angles. Both paths preserve initial offsets, turret identities, attached voxel barrels and
  pause/resume. Duplicate unnamed MDRN turret infos share one initializer. Nothing changes
  live actor facings, movement, attack behavior or shared rules.

## Fixed weapon sockets and remaining platform work

The [first fixed weapon sockets](fixed-weapon-sockets.md) are now implemented, replacing the old
2×2 weapon block/red front-column rule with exact-fit footprints:

- Typical cannon socket: **4×1**, protruding forward from the turret.
- Sonic Turret socket: **5×1**, across the top, inside the turret silhouette, colored red.
- **Still pending — Mammoth Mk. II:** a single large **four-legged fixed platform**, no separate turret selector
  or turret container; **two cannon sockets and one missile-launcher socket**.

Mammoth's original Railgun.MKII channel has two muzzle offsets. Dragon.MKII and RedEye.MKII
are separate ground/air channels of its missile package; these must not become two invented
launcher slots or extra weapons. Separate sockets must not double the inherited firepower.

Socket matching must distinguish weapon class from turret class and grid cell size. Independent
weapon swapping will still need explicit weapon/art/trait bindings. Mammoth's generic walker view
does not yet implement the separate three-socket fixed-platform revision.

## Validation

Native tests cover declared classes, mismatched-class rejection, narrow legacy migration, original
weapon packages, portrait placement bounds and counter-rotation timing. The engine utility checks
sprite/voxel turret initializer construction across all 89 bindings, including duplicate MDRN infos,
as well as original versus compiled image bindings. Automated checks are not live visual acceptance.
