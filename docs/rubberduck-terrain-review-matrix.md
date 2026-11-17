# Terrain review matrix and missing mountain integration

## Visual review harness

`--rubberduck-rock-coast-playtest MAP OUTPUT overview` and `overview-shaded` make
visual-only review packages for any Rubberduck production preset. `detail-review`
and `detail-review-shaded` focus the first spawn. These modes add no movement
probes. They preserve terrain/resources, pin diagnostic fog/camera/faction settings,
and reject maps with custom rules or local artwork rather than dropping them.
The overview camera leaves space for the sidebar. Shaded reviews select the shaded
tileset on a copy, not on the source package.

`%TEMP%/ymca-terrain-review-matrix` contains 18 fresh exports and actual overview
captures: all nine presets, 4P seed 42 normal and 8P seed 43 shaded. Contact sheets
are `normal-contact.png` / `shaded-contact.png`. Four shaded first-spawn closeups
were also inspected. These are overviews and selected closeups, not exhaustive
inspection of every seam or gameplay certification.

## Defect found and fixed

The review revealed that Arabia, Battle Nexus and Hill Fortress exported planned
mountains as flat, impassable dirt-looking template 3992. The exporter only called
`RubberduckMountainRenderer` for Mountain Valleys. It now renders planned mountains
in every preset, using the existing shared height-four blocked surface renderer.
It rejects attempts to raise non-cliff or resource-bearing cells.

This is **not purely a color change**: existing blocked cells receive height four.
Terrain templates/indices, resource bytes and every passable cell's height stay
unchanged. No ramps or additional blocked apron cells are introduced. Existing
blocked roofs stay blocked. Terrain elevation can affect sight/projectiles; ground
route evidence below is not a full combat/visibility approval.

`PlateauPathFinder` formerly enabled its corner safety only when a map contained
ramps. It now enables it for elevated Rubberduck terrain too; other tilesets are
excluded. Without this, the first Hill Fortress runtime exposed a unit cutting a
raised corner. The fix retains the existing cardinal-intermediate/detour policy,
not relaxed collision.

## Evidence for the corrected exporter

Root: `%TEMP%/ymca-mountain-integration`.

- 18 exports: same preset/player/seed matrix as above. The 12 exports belonging to
  the six unaffected presets are `map.bin`/`map.yaml` identical to their predecessors.
- The six changed maps retain identical tile and resource data. Height differences
  are exclusively 0 -> 4 on previously blocked 3992 cells (`terrain-diff.txt`):

| Preset | 4P42 cells | 8P43 cells |
|---|---:|---:|
| Hill Fortress | 738 | 1007 |
| Battle Nexus | 1132 | 1820 |
| Arabia | 780 | 1004 |

- `review-final` in each of those six map folders contains actual replacement
  overview captures, normal/shaded respectively. `final-contact.png` was inspected.
  These supersede the flat-brown-ridge images in the earlier review matrix.
- `hill-fortress-4-42/play-final`, `battle-nexus-4-42/play-final`, and
  `arabia-4-42/play-checked`: 16 actual ground movement legs and eight blocked
  mountain-entry checks each. Infantry and tanks travel between four spawn areas
  and return. Every visited physical cell is checked for height zero and no cliff
  or ramp entry; each planned path is checked likewise. Fixture terrain is
  byte-identical to its production source.
- The `ground-routes` utility mode reproduces those movement fixtures. Successful
  route logs, not the screenshot's earlier capture tick, establish completion.
  Arabia's probe records physical target visits before waiting for idle, so a
  subsequent legitimate vehicle nudge does not erase an arrival. It never accepts
  merely stopping near a target. Earlier strict idle-at-target runs stopped
  adjacent to an enterable destination and timed out; retain them as diagnostics,
  not final passes. `play-safe` was interrupted by the screenshot harness timeout.

The installed 4P42 Arabia, Battle Nexus and Hill Fortress packages are refreshed
from these exports. Existing arbitrary authored maps are not rewritten. Bridges,
original assets and the fetched engine are unchanged.

## Still not an all-clear

The renderer now represents the three presets' intended mountain barriers, but
raster stair-steps, some side/foot joins, map-edge slivers, native spatially phased
water, and comprehensive shroud/projectile tests remain open. See
[terrain acceptance](rubberduck-terrain-acceptance.md). Native water's 6x6 phase is
spatial texture alignment; the source sheets do not establish animation frames.
