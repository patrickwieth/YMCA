# Building UI follow-up: China placement, GDI cameos and naval construction

Local Game worktree only; no release/push/deployment or user-game interruption.

## China

Advanced Nuclear Plant and Supply Depot move four native pixels downward in all
body states (idle, damaged-idle, place and dead). Final overrides live in
`sequences/building-position-feedback.yaml`:

- `chpower2.supplied`: Y -8 -> -4 at Scale 1.
- `chsupply.supplied`: Y 19 -> 22.2 at Scale 1.25, i.e. +3.2 source pixels = +4
  native screen pixels. No scale, footprint, source PNG or icon changes.

The radar-upgrade cameo now contains **both dishes**, using the bounded
`(0,0,60,50)` region of the existing China `radar.png`. The unrelated third object
on that source sheet is excluded. Source pixels are not repainted or invented.
The same unaltered crop is generated as
`bits/misc/overlays/china-radar-preview.png`, with transparent background, for
`chcommand.radar/idle`. This replaces the old `radarantenna.shp` placement image.
The 1.5-scale preview uses the supplied artwork; the actual command-center overlay,
plug requirements, power and upgrade behavior are unchanged.

The native check uses the real `PlaceBuildingOrderGenerator` over an owned command
center, not just a manually drawn actor thumbnail. A cropped native screenshot
shows both dishes in the placement ghost. (An earlier diagnostic rendering via
world annotations used the wrong coordinate space; it is not acceptance evidence.)

## GDI cameos

Only the portraits change, not live actors or their art/rotations:

- Weapons Factory source crop moves 24 pixels down to lift the motif in the cameo.
- Refinery source crop moves 20 pixels down.
- Barracks source crop moves 12 pixels down.
- Missile Tower is rerendered from its existing native multipart preview, body
  facing 0 and turret facing 597: approximately 30 degrees from facing the viewer,
  instead of the former 768 side view. The archived former composition is retained.

Catalog and provenance hold the exact crops and source hashes. The shared generator
reproduces the cameos and the radar's transparent placement source. The original
GDI PNGs/voxel art, including approved AA artwork, are untouched.

## Subsequent Nod cameo adjustment

At the user's request, War Factory and Repair Depot motifs sit higher in their
cameos: source crops `(45,26,319,231)` and `(13,54,179,179)` respectively.
Repair Depot received three further 12-source-pixel upward motif adjustments on request.
Only these two additional icon PNGs changed; live building positions are untouched.

## Subsequent Scrin cameo adjustment

Infantry Portal, Warp Sphere, Repair Bay, Gravity Port and Rift Generator source
windows move 20 pixels downward, lifting their motifs in the cameo. Manipulator
moves only 8 source pixels. Crop sizes, source artwork and live actors are unchanged.
Only the six requested Scrin cameo PNGs change; exact crops are in the catalog.

## Subsequent Soviet Power Plant cameo adjustment

The ordinary Soviet Power Plant motif moves higher using crop `(15,19,168,134)`.
Its source window moves down 16 pixels; source art and live building placement stay unchanged.
The Nuclear Powerplant's separate placement adjustment is in `soviet-nuclear-plant-art.md`.

## Final cameo follow-up

Cloning Vats moves substantially higher (crop `[30,48,226,196]`). Allied Ore
Purifier, Pillbox and Prism Tower also move higher; their explicit approved hash
transitions are in `approved-allied-icon-adjustments.json`. See
`building-finish-review.md` for the completed regression checks and remaining
building work.

## Naval make animations

Removed `WithMakeAnimation` from Allied Naval Yard, Soviet Naval Yard, Nod Sub Pen
and GDI Naval Yard. China inherits the animation-free Allied rule. The British
Fake Naval Yard is covered separately. No land building is changed.

The ready building appears directly when placed on water; no land construction
animation or its reverse sell animation plays there. Production queue build times,
prices, prerequisites, occupied cells and ship production traits are unchanged.
Unused sequence definitions are retained because removing shared art is unnecessary.

`BuildingUiFollowupProbe`, available only in an opt-in disposable fixture, creates
all five functional yards **without SkipMakeAnimsInit** on prepared water, checks
that they enter normal body states with no make trait, and captures each yard.
The fake yard is checked through the resolved-rule snapshot. All original map bytes
are preserved; water preparation runs only on a disposable `testmap1-debug.oramap`.

## Evidence

`art-source/building-ui-followup/` contains current resolved rules/sequences,
source-crop comparisons, all five naval captures, real radar placement capture,
native GDI Building/Defense menus, results and regression logs. All screenshots
used for acceptance were complete before the native fixture reported success.
The isolated build passed. Focused tests verify all six naval make removals,
unrelated rule preservation, four-pixel math, exact radar-source crop, the fifteen
requested faction cameo changes (including the later Nod pair, six Scrin portraits,
Soviet power portrait and Cloning Vats); later Allied adjustments have separate
explicit hash-transition checks and native results.
