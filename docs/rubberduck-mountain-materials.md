# Blocked mountain walls: authored material projection

The separate blocked-mountain renderer now uses its own four-direction wall
vocabulary, with three stable variants per direction. It no longer fills a full
wall by repeating a tiny 24x80 rock sample. Walkable plateaus, ramp strips and
coasts retain their existing artwork.

## Artwork and geometry

`--rubberduck-mountain-art SOURCE OUTPUT` derives `mountain-faces.png` from the
immutable `grassland/grassland_1x1.png` slots 5..7, 0..2, 12..14 and 8..10. This is
**projected derivative artwork**, not untouched authored cliff templates.

- Front faces map the original slanted rock plane onto the existing vertical wall.
- Sampling stays inside the authored relief: horizontal 20..80 percent, vertical
  10..90 percent; nearest opaque same-row sampling has a bounded 16-row fallback.
- The narrower initial inset produced dark serrated relief-edge artifacts at joins.
  Those candidates are not installed. The retained interior sampling removes that
  artifact in the actual close views without color-keying the rendered wall.
- Every pixel's alpha must equal the established wall mask. Roofs, wall silhouettes,
  anchor positions, direction/drop parameters and terrain-depth clipping are unchanged.
- New actors reuse `PlateauFaceBody` and its real editor preview/revision handling.
  Normal replanning recognizes the decorations and preserves the new material.
- Source and output hashes are installed beside the derivative. No ruin assets and
  no modifications to original sheets or existing plateau PNGs.

Files:

- `MapGeneration/RubberduckMountainRenderer.cs`
- `UtilityCommands/RubberduckMountainArtCommand.cs`
- `mods/ca/rules/rubberduck-mountains.yaml`
- `mods/ca/sequences/rubberduck-mountains.yaml`
- `mods/ca/bits/terrain/rubberduck/derived/mountains/`

## Verification

Evidence: `%TEMP%/ymca-mountain-finish`.

- All nine 4-player seed-42 exports have **byte-identical `map.bin`** compared with
  the preceding production exports. Heights, collision and resources are unchanged.
- Normalizing only the four new mountain actor type names back to their former
  full-height wall types makes every `map.yaml` identical too: actor IDs, anchors,
  owners, start positions, economy and rules are unchanged.
- Replacement counts: Arabia 636, Battle Nexus 836, Hill Fortress 694, Mountain
  Valleys 548. The other five exports are unchanged in both terrain and actor data.
- Final actual close views: `arabia-review/interior.png`,
  `hill-fortress-review/interior.png` (shaded), and
  `battle-nexus-review/interior.png`. Earlier `ingame.png`/`final.png` close views
  contain rejected sampling candidates and are not final visual evidence.
- `editor/ingame.png`: actual normal-editor preview/replan, five PASS categories
  including nonmutating draft, exact Undo/Redo and save/reload.
- `runtime`: production gameplay regression, 32 ascent/descent legs, 16 foot checks,
  MCV deployment. This preceded the final RGB-only inset; all alpha and gameplay
  geometry remained identical. The final editor and close views use installed art.
- The contour test also compares old/new mountain terrain, actor count, IDs, anchors,
  decoration recognition and exact material-only substitution on a notched fixture.
- Twelve generated sprite masks match the original wall geometry. All 33 original
  sheet hashes remain unchanged. Build and whitespace checks pass.

For a repeatable production close view:

```
--rubberduck-rock-coast-playtest MAP OUTPUT mountain-review
--rubberduck-rock-coast-playtest MAP OUTPUT mountain-review-shaded
```

This change does not smooth or widen the collision contour. Stair-step terrain
boundaries and the existing roof geometry remain deliberate; smoothing them would
be a separate topology change. Full zoom/filtering and GPU performance certification
are not claimed. Bridges remain separate until their sprites arrive.
