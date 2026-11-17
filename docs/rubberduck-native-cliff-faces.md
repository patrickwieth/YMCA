# Original-source cliff faces: first artwork integration

Production update: [complete native pieces are now selected on supported contours](rubberduck-native-cliff-production.md).
This document describes the earlier face atlas, which remains the fallback for other edges.

## Rear contact material correction

The rear outline previously sampled pale earth along the top of the original low
cliff. On the actual generated plateau this read as a bright, detached rail beside
the darker front rock. The installed atlas now retains the **exact existing alpha**
but samples RGB twelve source pixels inward into the same rear-facing rock piece,
with source x clamped to 32..95. An invalid inward sample falls back to the original
contact sample. No new colors, terrain cells, blockers or silhouette area are added.
This is a fitted derivative, not an unchanged native sprite.

The utility asserts all 150 front frames unchanged RGBA, all 150 rear alpha masks
unchanged, continuous contact and the existing six-above/two-below bounds. A trial
that enlarged the visible rim by changing vertical scale was rejected; scale two
and the old silhouette are retained in production.

Evidence: `%TEMP%/ymca-rear-contact/`:
- `vergleich.png`: side-by-side actual-client crop; baseline comes from
  `ymca-occlusion-band/checked-cliff-review/ingame.png`, accepted candidate from
  `rock-material/after.png`. Both use the same camera and production geometry.
- Root `after.png`: rejected thicker-outline trial, not production evidence.
- `rock-material`: accepted map-local preview and atlas assertions; the resulting
  atlas and provenance are installed under `bits/terrain/rubberduck/derived/`.
- `shaded/ingame.png`: installed shared-art shaded production closeup inspected.
- `editor`: nine planner preflights and **14 live shape-editor checks**, including
  native replan, rollback, Undo/Redo and save/reload; screenshot inspected.
- `production`: **32 movement legs, eight foot checks, eight height-edge checks**,
  home MCV placement/deployment under shroud; screenshot inspected. Terrain and
  production YAML are byte-identical to the previous 4P42 export.

This removes the pale contact material, not the angular contour. Native brown caps,
remaining side/end geometry and authored surf are still separate open items.

## Ramp retaining-wall material continuity (preceding milestone)

Short front profiles previously rescaled their texture by `max(dropA, dropB)` and
switched to `rock_cliffs.png` at two levels. Along a height-four plateau's ramp,
that made the same retaining wall abruptly change rock size/material as less of
it became exposed. All front profiles now sample the high sheet at a fixed
128-pixel vertical scale from the roof; their existing masks simply crop it.
This is a material correction, not smoothing of the terrain contour.

The utility validates all 150 front silhouettes against the installed atlas and
checks every visible RGBA pixel against the corresponding full-height profile.
During that update, the 54 front profiles whose maximum drop is four, and all
150 rear profiles, stayed RGBA-identical. Rear silhouette bounds/contact checks remain. The derived atlas
and its provenance are updated; no terrain, collision, actor definitions, original
images or historical tilesets are changed. This supersedes the old short-wall
sheet selection described in the historical implementation below.

Evidence: `%TEMP%/ymca-wall-strata/`:
- `candidate/ingame.png`: map-local closeup before installation; compared with
  `%TEMP%/ymca-rear-rims/play-normal/ingame.png`.
- `final`: atlas checks and production-map screenshot using installed art;
  `final/fixture`: 32 movement legs, eight blocked-foot and eight height-edge
  checks, plus actual home deployment.
- `editor`: 30 planner preflights and 23 live editor checks, including Undo/Redo.
- `play-normal` / `play-shaded`: 16 movement legs and eight blocked-foot checks
  each on the saved editor geometry; shaded changes only the tileset of a copy.
- Final normal/shaded, editor and generated screenshots inspected. Terrain bytes
  are preserved; this is not evidence that all geometric stair-steps, corner/foot
  joins or shroud cases are finished. No bridges were touched.

## Rear-rim correction (preceding milestone)

The original two-pixel rear strips sampled the **front** wall texture. Rear frames
now sample the original rear-facing rock silhouettes: slots 12–14 for direction 2,
8–10 for direction 3. These remain explicitly **derived, fitted** graphics, not
unwarped full native walls.

The silhouette is bounded to six pixels above and two below the existing roof
edge, tapering with the height drop. Source alpha defines the upper contour; an
inward opaque rock sample preserves the contact strip where leaning source ends
would leave holes. A taller rear billboard is deliberately not drawn through the
plateau roof. Runtime/editor terrain clipping stays active and unchanged.

The art utility checks all 300 frames: the 150 front frames remain byte-identical
RGBA to the installed atlas; rear profiles stay within the bound, have continuous
contact, and zero-drop profiles stay empty. No terrain, actors, collision or
resource changes are involved. Only the derived face atlas is replaced; original
sheets and historical tileset generation are untouched. Installed metadata is in
`derived/native-cliff-rims-provenance.txt` (sampling, source hashes, atlas hash).

Latest evidence: `%TEMP%/ymca-rear-rims/`:
- `final/rim-preflight.txt`: full atlas checks; `final/fixture` production 4P42 map
  ran 32 movement legs, eight foot checks and eight height-edge checks, plus home
  deployment. Installed-art screenshot is `final/ingame.png`.
- `editor`: 30 planner preflights and 23 actual editor assertions, including
  relocation, adding/closing ramps, resource protection and exact Undo/Redo.
- `play-normal` and `play-shaded`: 16 movement legs and eight closed-foot checks
  each on the saved editor geometry. Shaded input is a copy changing only Tileset,
  not a separate shaded editor session. These relocated layouts contain three
  distinct ramp orientations; the production run supplies the fourth.
- Final editor, generated and normal/shaded runtime images inspected; played
  terrain is byte-identical to its source. `candidate` is the earlier experiment
  with holes at leaning ends; `contact` is an intermediate closeup, not final logs.

The optional `[MAP]` argument reuses a shared-art runtime fixture instead of
creating a production fixture. Map-local sequence overrides are rejected rather
than silently discarded. This makes closeup comparisons possible without changing
terrain. It does not certify all shroud/occlusion combinations or all visual joins.
Angular rear contours, front/side end transitions and repetitive fallback walls
still need broader art work; bridges remain excluded.

## Implemented

`--rubberduck-native-cliff-art SOURCE OUTPUT` imports wall material directly from
`grassland/grassland_1x1.png` and `grassland/rock_cliffs.png`. It does not read `ruin`
or rewrite source files. It replaces the repeated 24-pixel sample formerly used by
plateau walls with larger orientation-specific original face regions.

- Southwest-facing source slots 0/1/2 and southeast-facing slots 5/6/7.
- Low-bank source for drops of one or two levels; high-wall source for larger drops.
- Three stable coordinate-selected variants per height profile; no simulation RNG consumed.
- 100 directional height profiles, 300 frames in total. Embedded metadata remains
  `FrameSize: 128,320`, `FrameAmount: 300`, `Offset: 0,96`.
- Source lean and height are normalized to the existing vertical terrain-edge geometry.
  RGB is sampled from the source interior, with nearest rock pixels extending small
  silhouette margins. Near-black opaque matte pixels (maximum RGB <= 8) are excluded:
  the initial candidate otherwise produced conspicuous black seams.
- Existing roof-occlusion clipping remains active. Expansion-valley cliff cells now use
  the same renderer/material as plateau walls; their collision and ring layout are unchanged.

**These are derived wall faces, not complete original corner/end sprites placed unchanged.**
The source silhouettes and proportions do not directly match the existing four-level
heightmap. Current geometry remains angular, and cliff-to-grass transitions remain hard.
Dedicated inner/outer corners, natural open ends and matching terrain transitions are
still outstanding in production. The first four unwarped corner/end assemblies have now
been verified separately; see [native cliff connections](rubberduck-native-cliff-connections.md).
This face-atlas integration should not be described as a finished native tileset.

## Source provenance

| Sheet | SHA-256 |
|---|---|
| grassland_1x1.png | 3B20F2709F2CF559D658E3C44DAA8B1461CD86FBA8A0D9D95CCC0F3EA2A8E8C3 |
| rock_cliffs.png | DBC784C8CBAED3212E5E9C4A02EF797A30BF77D06B47E12EA911FA8A5B43013D |

The utility also writes `native-cliff-frames.tsv` (every frame's source sheet, slot,
orientation, drop and variant) and `native-cliff-sources.sha256`.
`--rubberduck-source-lab` additionally exports individual high-slot inspection crops.
An adjacent-slot crop is not proof of a complete corner: some apparent pairs are two
opposite-facing wall sections. They must not be joined blindly.

## Build and test

Build CA, then use the normal engine utility environment:

```
OpenRA.Utility ca --rubberduck-native-cliff-art SOURCE OUTPUT [MAP]
```

The command produces an isolated `rubberduck-native-cliff-preview.oramap`, with map-local
sequence overrides and embedded candidate artwork. Its geometry comes from the ordinary
4-player Mountain Valleys generator. Install that diagnostic only temporarily.

After review, the production files are:

- `mods/ca/bits/terrain/rubberduck/derived/native-cliff-faces.png`
- `mods/ca/sequences/rubberduck-plateau.yaml` (from `native-cliff-sequences.yaml`)

The old plateau lab still exports its diagnostic texture; that is not the native-art
rebuild command. The 300-frame PNG is about 800 KiB compressed / 49 MiB uncompressed;
broader GPU-memory and long-match performance profiling remains open.

## Evidence

`%TEMP%/ymca-native-cliff-art` contains:

- `faces/ingame.png`: rejected first candidate, black matte seams.
- `faces/ingame-matte-fixed.png`: corrected walls, still mixed with legacy valley material.
- `coherent/ingame.png`: shared wall rendering on homes, islands and valley boundaries.
- `production8/ingame.png`: actual Operational 8-player export using installed sequences.

The coherent 4-player fixture and Operational 8-player run each passed **32 movement legs,
16 paired steep-edge checks, starting MCV placement and deployment**. Results are in the
safe log identified by each fixture's `runtime-results-path.txt`. Tactical 16-player
seed 44 also exported/reloaded successfully.

The 4-player map's `map.bin` is byte-identical before and after this artwork change:
terrain, elevations and resources were not changed to make the graphics pass.
CA Release build and `git diff --check` passed. This evidence does not certify full corner
composition, every shaded variant, or complete unit occlusion behind high ground.
