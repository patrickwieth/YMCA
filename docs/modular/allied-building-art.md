# Allied building artwork — isometric/modular

Worktree: Game / modular; production/news remain separate. Supplied PNGs are used as-is:
no source artwork is generated, recolored or resized. Never touch the protected Nod conyard PNG.
All Allied building PNGs and bibs use scale 1 (explicitly or the engine default).

## Current bindings and foundations

| Actor | Supplied artwork | Bib | Building body |
| --- | --- | --- | --- |
| PBOX | bunker.png | 1x1 | 1x1 |
| PRIS | prism tower.png | 1x1 | 1x1 |
| GAP | euro_forces_gap_gene...-1070714859-0.png | 2x2 | 2x2 |
| POWR | small fusion power.png | 3x3 | 2x2 |
| APWR | fusion power 1.png | 3x3 | 2x2 |
| TENT | barracks.png | 3x3 | rear 3x2 |
| PROC | refinery.png | 3x3 | original refinery footprint/docking |
| WEAP | war factory2.png | 4x3 | rear 4x2 |
| FACT | conyard.png | 4x4 | original 3x3 |
| OREP | purifier.png | 3x3 | 2x2 |
| PDOX | chronosphere.png | 3x3 | 2x2 |
| WEAT | weather control.png | 3x3 | 2x2 |
| ATEK | tech center.png | 3x3 | 2x2 |
| HPAD | helipad.png | 3x3 | 2x2 |
| DOME | radar.png | 3x3 | 2x2 |
| FIX | service depot.png | 3x3 | original passable repair footprint |
| AFLD.allies | airfield.png | 4x3 | rear 3x2 gameplay body; 4x2+ decoration |

The user supplied `bib4x4.png` (512x256) for Allied Conyard. War Factory initially used it too,
then the user chose the smaller `bib4x3.png` for both War Factory and Airfield. The 4x3 source
is 447x224, used unchanged at Scale 1 (no padding/stretching to 448). All three retain their
original solid bodies with driveable apron cells. Legacy tiled Allied bibs are removed. HBOX is unchanged.
Prism Tower2.png is reserved for later, not a damaged/charging state.

## Driveable front bib edges — approved gameplay change

The user explicitly requested that the lower-left and lower-right bib tiles remain traversable
by vehicles and infantry for 2x2 bodies on 3x3 foundations. `^Allied2x2DriveableBib` now defines:

```
xx=
xx=
===
```

`x` is occupied building body; `=` is OccupiedPassable (NOT a solid tile or a transit-only tile).
Thus the site reserves 3x3 placement cells but only four cells block movement; five bib cells
are pathable, covering BOTH front-facing edges. The collision shape is also 2x2. This is not
merely a larger decorative PNG.

Applies to POWR, APWR, OREP, WEAT, ATEK, HPAD, DOME and PDOX. Body LocalCenterOffset is 0,-724,0;
full-bib overlay WORLD offset is 0,724,0, configured as body-local Offset -1134,0,0.
The body sits 32 projected source pixels behind the full-bib
center. This replaces the earlier centered decorative-only foundation policy. Selector shapes
and existing fine offsets remain attached to the body. Power/cost/health are not changed.

**APWR no longer blocks 4x4 cells:** its former 4x4 site is replaced by this 3x3 site / 2x2 body
under the approved passable-edge policy. Power remains 200 (small plant 100). Soviet APWR and
TPWR explicitly retain 4x4 collision/sites; Soviet POWR retains 2x2, DOME/HPAD retain 2x3. Their
old bibs and decoration settings are isolated from Allied inheritance. Soviet HPAD uses native
`hpad` artwork, not the newly supplied Allied PNG. Nod remains unchanged.

GAP retains its special 2x2 site/bib. WEAP retains its separate rectangular-body policy.
The refinery's special harvester footprint is not replaced by this generic template.

## Rectangular buildings

- Barracks: user-expanded 3x2 body, collision and footprint-derived selector on the existing 3x3
  bib/site (`xxx xxx ===`, six solid/three passable cells). LocalCenterOffset 362,-362,0 moves the
  actor 32px right and 16px down relative to the former 2x2 body. Bib body-local -567,-362,0 keeps
  the foundation stationary. Both infantry exits remain in the driveable front row. Selector
  height remains 64. The user replaced the briefly supplied 256x256 canvas with a cropped 217x146
  PNG during this adjustment; therefore no additional 72px downward image offset is applied.
  Image Offset stays 4,-7 at Scale 1, with no assistant modification to the original PNG.

- Chronosphere: the user reverted its body/selector from 3x2 to 2x2. It now uses the shared
  `xx= xx= ===` 3x3 site and 2x2 collision/selection, with five pathable bib cells. The shared
  2x2 body/bib registration replaces its old rectangular-body center correction. DecorationHeight
  is now 105 after the user's additional 25% increase. Image fine Offset is now -4,35; original PNG stays scale 1.
- War Factory: site 4x3, `xxxx xxxx ====`, unchanged rear 4x2 collision/selector and exit at
  1,2. Eight solid/four pathable cells. LocalCenterOffset 362,-362,0 preserves the old actor pivot
  (724,1448 relative to the origin cell). The 4x3 bib uses body-local Offset -567,-362,0 -> world
  -362,362,0. Soviet WEAP retains its old site/collision/bib.
- Allied Conyard: site 4x4, `xxX= xxx= XxX= ====`, retaining the original nine solid cells and their
  targetability, plus seven pathable apron cells. LocalCenterOffset -362,-1086,0 preserves the old
  actor pivot (-362,1086 relative to the origin cell), so the tuned image and selection do not
  move. Bib body-local Offset -1701,362,0 -> world 362,1086,0 centers it on the 4x4 site. Collision,
  transform offset, PNG offsets and selector remain unchanged. Allied Nukular Conyard inherits
  this binding; both Soviet Conyards explicitly retain their old 3x4 site, center and tiled bib2.
- Allied Airfield: 4x3 bib/site, `xxx= xxx= ====`, unchanged 3x2 occupied body and mouse bounds.
  Its decoration alone now uses a 4x2 footprint plus a quarter-cell extension along the second axis. Six solid/six pathable cells. LocalCenterOffset 0,-724,0 preserves its previous actor
  pivot (362,1086 relative to the origin cell). Bib WORLD offset 0,724,0, configured body-local
  as -1134,0,0. No Soviet/GDI airfield change.

## Fixed coordinate-space error: buildings appearing far left of their bibs

WithIdleOverlay.Offset is BODY-LOCAL, unlike Building.LocalCenterOffset (world-space).
The renderer calls BodyOrientation.LocalToWorld after rotating by the actor orientation.
For these standing buildings the default classic-perspective transform is
`world = (local.Y, -654 * local.X / 1024, local.Z)` with integer truncation.

The earlier mistaken local offset 0,724,0 therefore moved the bib 64 source pixels RIGHT,
not 32 pixels down. This made the building appear far left of its foundation. The corrected
local -1134,0,0 gives precisely world 0,724,0: no horizontal displacement. The previous 3x3-site
Airfield (and previous 3x2 Chronosphere) required local -567,-362,0 for world -362,362,0. Those
bibs were also 64 pixels too far right. The current 4x3-site Airfield and 2x2 Chronosphere use
-1134,0,0; the current 4x3-site War Factory uses -567,-362,0. This fix changes the erroneous bib registration, not building sprite X offsets,
selectors, occupied/pathable cells or the separately requested Chronosphere image adjustment.

The C# regression now executes the actual resolved BodyOrientationInfo.LocalToWorld conversion,
rather than comparing raw offsets as if they were world vectors. It failed on the old rules
before the fix. Existing passability and selection checks remain enabled. Live recheck pending.

## Fine registration and latest visual pass

Image bindings live in `sequences/allies.yaml`; **final absolute pixel overrides** live in
`sequences/allied-building-placement.yaml`, loaded LAST after all faction sequence files. This
also keeps Soviet overrides after their base inheritance declarations. Edit the latter file for
fine adjustments. Idle/damaged/active variants move together; the engine audit verifies the
fully merged sequence manifest. Older offset values in the base file are intentionally overridden.

Latest downward fine adjustment after the bib-coordinate fix (source-image pixels):
- HPAD: 10px right and 5px up; final Offset 0,7.
- War Factory: idle/damaged/place PNG states move 10px left and 10px up from 0,27,
  final Offset -10,17. Bib, actor center, selector and blank door overlays are unchanged.
- Allied Conyard: supplied idle/damaged PNG states move another 4px up from
  the previous 40,1 trial, final Offset 40,-3. No footprint/bib/selection or native SHP animation change. Soviet
  conyard inherits fact, so its idle/damaged offsets explicitly retain 30,-45.
  Its two inheritance keys in soviet.yaml are named Inherits@FACT / Inherits@MAKE: repeated
  bare Inherits keys crash MiniYaml.MergePartial when that root receives a cross-file override.
  A static regression checks unique keys on overridden sequence roots; the full Windows
  --check-only launcher also verifies sequence merging during real map preparation.
- DOME: another 10px down, final Y 17; Allied display name is Radar Station (Soviet Radar Dome unchanged).
- ATEK/POWR: another 4px down, final Y 32/38. APWR: another 4px down, final Y 29. These image-only moves leave selectors and bibs unchanged.
- TENT: Offset 4,-7 retained for the user's cropped 217x146 PNG. Its new 3x2 body center moves
  the graphic 32px right / 16px down; no extra canvas compensation is needed.
- FIX: another 4px up, final Offset 6,-45; previous left shift retained.
- SYRD: another 20px up, final Offset 0,-52; confirmed selector and water terrain unchanged.
- OREP: another 4px down, final Y 32.
- PRIS: another 4px up, final Offset 0,-6.
- WEAT: 4px right and 8px down, final Offset 4,15.
- Selectors, body centers, bib registration, shadows and pathable cells are unchanged.
- PDOX: another 20px straight down from -4,15, final Offset -4,35. Selector height 105,
  ground corners, bib and occupancy remain unchanged by this image-only move.
  Its body/selector was subsequently reverted to 2x2 using the common driveable-bib template.
- Airfield image Offset remains -20,-20. The user subsequently supplied and selected a 4x3 bib,
  superseding the earlier pending 3x4 request. Its 4x3 site preserves the occupied 3x2 body,
  actor pivot; no image shift, scaling or stretching accompanies the bib change. The subsequent
  decoration-only enlargement is documented below.

Retained selection decisions from live review:
- Allied Conyard (including Nukular variant): DecorationHeight doubled from 30 to 60 at the
  user's request. Width, ground corners, bib and image registration stay fixed; Soviet Conyards
  explicitly retain height 30.
- APWR/POWR decoration rectangles have Y offset -181 world units (8px up). APWR height is
  90 (increased from 74); POWR height is 69. TENT now has a footprint-derived 3x2 selector, height 64. POWR was increased by another
  50% (46->69) at the user's request; width, ground corners and image registration stay fixed.
- PROC bounds 4344,4163,0,-91 retain the old top while trimming the lower edge upward by 8px;
  height is now 55 following the explicit +25% request. The ground diamond remains fixed.
- PBOX image remains Offset 0,-8; its box/height 76 were explicitly confirmed and are unchanged.
- PRIS image remains Offset 0,-6; decoration height is now 171 (+10%).
- SYRD's selector ground corners remain fixed; height is now 60 (+100%), with no concrete bib.
- OREP image is now Offset 0,32 after the latest 4px downward adjustment.

Chronosphere and Radar now use the user's 256x256 canvases. Radar decoration height is 103 after
the user's replacement of the old 1024px PNG. No image is silently rescaled. Construction/death
sequences remain inherited; static replacement artwork is reused for active/damaged states
where no separate supplied design exists. Tech Center covers false-active states too.

Numeric registration checks do NOT prove that a live visual result is aesthetically correct.
Further user review is expected; no successful screenshot review is claimed.

## Latest user-requested selection sizes

Heights are adjusted from their current values, rounding half pixels upward:

| Actor | Previous height | Current height | Request |
| --- | ---: | ---: | --- |
| Refinery | 44 | 55 | +25% |
| Airfield | 66 | 83 | +25% |
| Naval Yard | 30 | 60 | +100% |
| Chronosphere | 84 | 105 | +25% |
| Gap Generator | 146 | 183 | +25% |
| Tech Center | 86 | 108 | +25% |
| Service Depot | 30 | 38 | +25% |
| War Factory | 38 | 76 | +100% |
| Prism Tower | 155 | 171 | +10% |

All existing ground corners remain unchanged EXCEPT the explicitly enlarged Airfield decoration.
Airfield uses additive optional engine fields `DecorationFootprint: 4,2` and
`DecorationFootprintExtraDepth: 181`. The four-cell axis extends from the original minimum occupied
cell. The extra depth moves the positive-second-axis edge by approximately 16px left and 8px down
(one quarter cell). This is a genuine isometric 4x2 rectangle, not a symmetric diamond pretending
to be one. Only decoration rendering uses these fields; mouse bounds, collision, occupied cells,
exits, 4x3 bib, actor center and PNG placement remain unchanged. Defaults preserve existing engine
behavior for every other actor. Soviet Refinery/Service Depot/War Factory and Chinese Naval Yard
explicitly retain their previous heights. No artwork is changed.

## Automated selection-height review (initial measurements)

`--export-allied-selection-geometry` exports resolved decoration/PNG registration for 18 Allied
buildings on flat terrain. `python tools/audit_allied_selection_heights.py geometry.json --check`
reads each original PNG's alpha bounds and reports boxes whose top is more than one source pixel
below the visible image top. It accounts for Center/Bottom anchoring, image offsets, explicit
selection offsets and footprint-derived selection corners. It does not infer correct footprint
widths, identify roofs versus antennas, or certify the full polygon/animation alignment.

Nine DecorationHeight values were increased: PRIS 143->155, POWR 42->46, TENT 48->64,
GAP 122->146, PDOX 64->84, WEAT 75->81, ATEK 78->86, DOME 93->103, AFLD.allies 30->66.
Only the tops move upward: ground corners, rectangle offsets, gameplay, images and shadows
remain unchanged by these height edits. Taller existing boxes are NOT shrunk automatically.

PBOX, SYRD and PROC remain explicit review-only exceptions because the user previously confirmed
their selectors or requested a fixed top. Their diagnostics still flag shortfalls, but --check
does not fail for them. See `selection-height-audit.md` for the measured results and limits.

## Full selection-shape review

The geometry export also includes all six selection-polygon vertices, the four ground corners
and body cell dimensions. `tools/audit_allied_selection_shapes.py` compares alpha coverage
against the full polygon and creates an optional HTML overlay report without modifying images.
It distinguishes roof-edge from ground-edge excursions, horizontal overhang and relative centers.
See `selection-shape-audit.md` and `allied-selection-shapes.html` for the current review.
This audit made no further changes to selector sizes or gameplay; apparent size mismatches need
visual interpretation because shadows/overhangs and ground footprints are not identifiable from
alpha bounds alone.

## Resource silos disabled

SILO, SILO.Soviet, SILO.TD (GDI/Nod) and SILO.SCRIN retain their actor IDs for existing maps,
but each Buildable has ~disabled, excluding resource silos from construction for all factions.
Missile/nuclear silos MSLO, MSLO.Nod and chsilo are NOT disabled. Storage/economy values elsewhere
are unchanged. AI production still filters through buildable prerequisites.

The disposable building-preview map removes all resource-silo actor blocks (currently four),
without renumbering/moving any other actors. Missile/nuclear silos remain. The original archive
is preserved; the map-preparation test compares complete surviving actor definitions and its hash.

## Test map, water and turret

`Game/test-gebaeude.cmd` builds and opens the disposable building-test map. `--check-only` performs
build/preflight/map preparation without starting the game. The original archive stays untouched.

The disposable copy replaces the old Allied gun slot with pris, preserving actor ID/owner/location.
Allied GUN/Turret is disabled in the build menu (~disabled), but remains for map compatibility;
GUN.Nod and unrelated turret mechanics remain intact.

`--prepare-building-test-water` adds actual water terrain under the Naval Yard with a one-cell
border for ship exits (25 cells in this map). It reuses an existing water tile, or a single-cell
water template from the current tileset. Only `testmap1-debug.oramap` with the expected title is
accepted. Original-map hash is checked in launcher testing. Normal maps are not rewritten, and
no artwork is generated. Naval Yard already requires Water terrain for normal placement.

## Automatic shadows — all 18 Allied PNG building types

The engine already provides WithShadow (IRenderModifier). It duplicates non-decoration renderables,
uses ReplaceColor plus alpha for a dark silhouette, and offsets that copy beneath the body.
WithIdleOverlay bibs are decorations and therefore excluded. This works without modifying or
creating source PNGs. Its Offset is world-space (OffsetBy), unlike WithIdleOverlay's body-local
Offset; do not reintroduce the coordinate-space mistake above.

Latest user adjustment: sun upper-center, with no sideways shadow displacement.
Shared ^AlliedBuildingShadow defines ShadowColor 00000070 (black, 112/255 opacity), WORLD Offset
0,90,0 (approximately 4 source pixels straight down), ZOffset -2048 (below body, above bib),
RequiresCondition !build-incomplete. This replaces the previous 4-right/2-down trial.

Applied to FACT, PROC, WEAP, PBOX, PRIS, POWR, APWR, TENT, GAP, OREP, PDOX, WEAT, ATEK,
HPAD, DOME, FIX, SYRD and AFLD.allies, plus the inherited Allied FACT.Nukular variant.
Soviet derivatives (including TPWR and both Conyards) and Chinese CHSYRD explicitly remove
the inherited shadow. Legacy buildings outside this PNG set are not changed. Original PNGs,
bibs, selection rectangles and gameplay are unchanged by this shadow trait.

This remains a simple displaced silhouette/drop shadow, not a perspective-projected or physically
accurate ground shadow. The higher-sun effect is approximated by a shorter displacement. Visual
review of the short straight-down version across all 18 building types is pending. The audit checks
scope, opacity and placement; it does not substitute for live visual verification.

## Team colors — still a proposal

Await user-supplied transparent masks on visible front bib edges/corners. Covered strips cannot
be seen under the building; around 6–8 source pixels of visible width work at viewport scale 0.5.
Match bib canvas/registration. White/grayscale masks can later be technically converted into
player-remapped layers. Ordinary RGBA PNGs do not remap merely by choosing a player palette.

## Checks

- `--check-allied-building-art`: resolved images, merged offsets, bibs, selection policy, dimensions,
  exact occupied/passable cells, actual BuildingInfo.OccupiedTiles/PathableTiles counts, 2x2 collision
  shapes, centers, factory exit, airfield front row, Soviet isolation, scale 1 and power output.
- `--check-selection-traits`: unique ISelectableInfo and actual mouseover-priority lookups.
- `python -B -m unittest discover -s tools -p test_allied_building_alignment.py`: alpha bounds and
  recorded art shifts, refinery lower-edge trim, rear centers and driveable-bib layout. Images read only.
- `test-gebaeude.cmd --check-only`: real Windows build and disposable-map preparation.

A live vehicle/infantry crossing test is still needed; rule/pathable-cell audits are not a live match.
Restart the game for visual review. No push, release or deployment is implied.
