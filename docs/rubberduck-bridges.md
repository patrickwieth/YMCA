# Rubberduck six-part bridges

## Implemented scope

Newly generated **Continents** maps now use real, destructible bridge actors in
place of the old indestructible sand crossings. Tactical and Operational remain
supported; Continents is still unavailable in Strategic. Existing saved maps are
not rewritten. Other presets keep their established topology: in particular,
Migration isolation and Archipelago teammate-only shallows are not replaced with
new inter-island bridges.

This is a **ground-level bridge**, using the existing engine `GroundLevelBridge`
movement/health lifecycle. Units walk on the height-zero deck between the two
sprite layers. The piers provide visual elevation, like the existing flat-bank
terrain. There is **no second movement layer or ship underpass while intact**.
After destruction the actual underlying water becomes available again. Elevated
plateau-to-plateau bridges, partial-span destruction, repair huts and separate
rubble/damaged artwork are not claimed by this implementation.

## Artwork and drawing order

The six supplied indexed PNGs in `mods/ca/bits/terrain/environment` are read
directly, without rewriting or recoloring their pixels. Each has its own embedded
palette; assuming RGBA/no palette made the initial diagnostic invisible and was
corrected with `PaletteFromPng`.

All parts retain their shared **256x256 canvas and origin**. For one center repeat,
all six image origins coincide. Additional centers repeat together at source
pitch **75,37.5**, not 256 pixels. Only the right cap and its matching frame move
to the last repeat. Half-pixel progression is retained until projection, rather
than accumulating a rounded 37/38-pixel step.

One common uniform scale fits the whole assembly to the native cell span; no part
is scaled or anchored independently. The perpendicular orientation reflects the
whole assembly horizontally, including its frame. It is a mirrored use of the
supplied art, not an additional authored sprite family.

`RubberduckBridgeBody` caches runtime renderables and uses separate sorting offsets:

- Deck: -2172 world sorting units.
- Actual vehicles/infantry: their normal renderers and positions.
- Steel frame: +2172 world sorting units.

The same composition is used by editor previews. Bounds include the common scale.
These offsets control drawing, not physical terrain height.

## Topology and export

`RubberduckBridgePlanner` searches straight native X/Y cell spans, with land ends,
land approaches and water between them. Endpoint ownership comes from a flood of
actual continent land, not distance to its center. Resources, generators, home
clearance and tech locations are protected before selecting the spans. Banks are
reserved against later rock-coast placement.

Two-continent maps commit **two equal-length, exactly half-turn-related spans** as
one transaction. Unsupported candidates retry the complete deterministic layout.
For custom three/four-continent layouts, the required continent ring links are
selected before any crossing is committed. Length is bounded by the map, not an
arbitrary center-piece limit.

`MapPlan.Clone` preserves the immutable span descriptions in an independent list.
Export writes water below the internal bridge cells and ordinary land at its ends,
then emits a passable building footprint and the bridge rules/actor. Guards reject
lost span metadata, invalid underlying water/land, elevation or economy conflicts.
The saved map roundtrip validates its custom rules and terrain. The ordinary map
wizard already uses this exporter, so no diagnostic flag is needed to enable bridges.

`GroundLevelBridge` installs the walkable custom terrain while alive and restores
water on removal. It kills stranded ground traffic. `RubberduckBridgeCollapse`
then removes only dry-land husks that those deaths spawn on the restored water;
otherwise a floating tank wreck incorrectly blocks the new naval passage. It does
not delete unrelated actors or modify ordinary unit/husk rules.

## Completed tests

Utilities run from `Game/engine` with the established `MOD_SEARCH_PATHS`/`ENGINE_DIR`.

```
--rubberduck-bridge-plan-test
--rubberduck-bridge-test OUTPUT [shaded] [focus=x|y]
--rubberduck-bridge-playtest MAP OUTPUT [editor] [shaded]
--rubberduck-naval-playtest MAP OUTPUT
```

Evidence root: `%TEMP%/ymca-bridges`.

- `bridge-plan-tests.log` in `%TEMP%`: **40 cases, each generated twice**; Tactical /
  Operational, 2/4/8/12/16 players, seeds 42/43/101/202. Checks exact bridge-pair
  symmetry, deterministic terrain/economy/spans, dry ends, water interiors,
  protected resources and independent cloning.
- `symmetric`: **24 actual exports** (20 Tactical plus four Operational).
  `multi-team`: another **four exports** for three/four teams and seeds 42/202.
- `final-x`, `final-y`: real normal/shaded client sessions, each covering lengths
  3/6/12 in both orientations. Each records **36 movement checks, six real vehicle
  layer-order checks and six collapses**.
- `accepted-production-x`, `accepted-production-y`: actual generated 4P42/4P202
  maps, each with **12 movement checks, two layer checks and two collapses**.
  Binary terrain and bridge actors/rules are preserved by the diagnostic wrapper.
- Across those four sessions: **96 movement checks and 16 layer/collapse checks**.
  Movement includes tank and infantry crossings in both directions, parking a
  tank on the deck, and actual DD passage after collapse. Naval entry while intact
  is explicitly rejected. To keep the weapon test short, the diagnostic reduces
  bridge health to 1000; **actual Challenger weapons destroy the remaining HP**.
  Production bridge HP remains unchanged at the inherited 100000.
- `accepted-editor-x`, `accepted-editor-y`: actual normal/shaded editor captures, **ten PASS
  checks** in total: runtime/preview anchors and layers, exact Undo, Redo,
  restoration and terrain/actor/rule save/reload. This validates existing bridge
  actors; it is not a new interactive variable-length bridge brush.
- `accepted-naval-x`: **four docks, eight normal ship productions and sixteen
  reciprocal naval legs** on the final bridge-enabled production map. The naval
  fixture now preserves generated bridge rules instead of dropping them.
- `other-presets`: all eight other 4P42 presets retain byte-identical `map.bin`
  against their earlier production baselines.
- Release build, whitespace check, island-economy regression and all 33 previously
  catalogued immutable Rubberduck source hashes pass.

Initial invisible-palette captures, the pre-cleanup floating-wreck failure, and an
early diagnostic that waited from the attack order instead of actual collapse are
not counted as passing evidence. The earlier `matrix`/`production-play` layouts
predate atomic mirrored-pair selection; final generator evidence uses `symmetric`
and `current-production`.

Terrain-only naval graph audits ignore actor-provided custom terrain; they cannot
certify passage through an intact bridge. Use the runtime naval probe for those
maps. No new long-match, WAN, hardware or universal competitive-balance claim is made.

## Inland landings follow-up

Generated caps now sit one cell farther inland, with two dry footprint cells at
both ends. Two-continent maps require an existing three-wide landing pad and two
approach cells. Compact custom three/four-continent rings use a single-lane pad
and one approach cell: imposing the larger pad prevented 3-team/seed-42 generation.
Both variants retain the inland anchor, protected economy and full-ring commit.
No water is filled and no blocked ground is made passable. Existing dirt material
covers the dry bridge ends and short approaches; no additional artwork is needed.

`BridgeDeckRenderable` hides only lower pier pixels behind ordinary dry ground.
The road/rail band, common canvas registration and frame layer are preserved.
Column-continuous clipping avoids isolated pier feet reappearing beyond a jagged
bank. UV rectangles reuse the source sheets, with no edited PNG or per-cell texture.
Runtime and editor caches follow terrain revision changes.

Follow-up evidence: `%TEMP%/ymca-bridge-landings`:

- `--rubberduck-bridge-plan-test`: 40 twice-generated symmetric cases plus four
  twice-generated compact ring cases; the previously failing 3-team seed passes.
- `--rubberduck-bridge-bank-test`: 48 masks covering both axes, three scales,
  repeat positions, grass/dirt/water/blocked ground; road preservation, rectangle
  coverage and no detached lower fragments.
- Shore-art regression: 1880 source/coverage checks after generalizing mask sizes.
- `accepted-x` / `accepted-y`: 24 movement, four layering and four collapse checks.
  Final 4P42/4P202 production terrain and YAML match these captured fixtures.
- `editor-x` / `editor-y`: twelve PASS checks, including bank painting followed
  by exact Undo/Redo and immediate clip-cache invalidation, then save/reload.
- `naval`: four docks, eight productions and sixteen reciprocal legs.
- Release build and twelve island-economy cases pass. Custom ring exports pass
  for 3/4 teams and seeds 42/202; these are not additional visual approvals.

Actual client screenshots: [normal X](images/bridge-landings-normal.png) and
[shaded Y](images/bridge-landings-shaded.png). These show improved contact, not a
claim that every coastline or the remaining terrain acceptance matrix is complete.

## Supplied source fingerprints (SHA-256)

| File | SHA-256 |
|---|---|
| bridge-left.png | bf514bdaa9927f4ff74f687b6825f5574ae8fe7a65465241befbb4ee467155a6 |
| bridge-center.png | 2ed7043394cd4bc5e985905d8d215b130cb2cbc7a844727336e77eced929d50a |
| bridge-right.png | ebde233f075307014db73bfdb2cca014acd7da265ea2dbcebe8b753199a9a222 |
| bridge-frame-left.png | 467ce8b8b3c98209587e165c03bbcf4c6c42d837f86114cb9b8c387eebc6e6ed |
| bridge-frame-center.png | 43e1d3cda05c1d8c25f04d48821d69ba0203709e3ea91368950b5da67ec2b4c5 |
| bridge-frame-right.png | 18ba36569f8f2a1ba515526345c6839ea9e2d7f608cf3308f5fe72d08c9a5d21 |
