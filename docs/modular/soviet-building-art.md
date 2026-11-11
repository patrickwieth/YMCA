# Soviet building separation and foundations

Current alignment work is local on `Game / modular`; no new commit, push or release.

## Independent buildings

`rules/soviet/building-bases.yaml` supplies seven independent Soviet bases for
FACT.Soviet, POWR.Soviet, APWR.Soviet, WEAP.Soviet, FIX.Soviet, DOME.Soviet and
HPAD.Soviet. They no longer inherit concrete Allied actors or Allied visual
traits. Initial gameplay definitions were copied from the shared rules at
3e2b7927, with the existing Soviet faction overrides retained. Shared generic
building/production traits remain shared. This intentionally allows future
Allied and Soviet building edits to diverge.

Soviet conyard placement has moved out of `allied-building-placement.yaml` into
`sequences/soviet-building-art.yaml`. FACT.Nukular.Soviet inherits the Soviet
conyard and its foundation. The helipad now has its own `hpad.soviet` image key;
no Soviet helipad PNG exists yet, so this key inherits the original SHP helipad,
not `hpad.allies`. AFLD now directly uses `afld.soviet`, including when captured
or previewed outside the original Soviet factions.

## Bibs and placement

All supplied 1x1/2x2/3x3/4x4 Soviet bibs are registered at Scale 1. Current use:

- 4x4: conyard (including Nukular), nuclear/tesla power plants, factory,
  repair depot and naval yard.
- 3x3: ordinary power plant, radar, refinery, cloning vats, North Korean bunker,
  missile silo, Iron Curtain, airfield, tech center, barracks, industrial plant
  and helipad.
- 2x2: legacy sentry/tesla turrets and SAM/flak base.
- 1x1: Tesla Coil; the native32 Sentry also uses 1x1 in the disposable test map.
- 1x1: registered but not currently assigned.

These are decoration-only overlays. Legacy SHP bib traits are removed entirely;
the refinery no longer inherits an Allied bib. Disabling a legacy bib with
`RequiresCondition: false` is insufficient: `AddedToWorld` still loads its sequence.
This caused the reported `powr.soviet` / missing `bib` startup crash and has been
corrected for every Soviet PNG-bib actor. Body PNGs use Bottom anchoring and initial rearward placements to
expose the front bib edges. The Sentry/TTUR rotating sprite-sheet registration
is deliberately left unchanged; only their foundations change. Helipad graphics
are likewise unchanged pending a new asset. No PNG bytes were edited or resized.
Resource silos remain disabled and keep their old graphics.

The missile silo retains its seven-frame opening animation; idle/open/damaged
states use matching placement. The small Soviet power plant now also has the
proper PNG in its damaged state instead of falling back to the old graphic.

## Earlier integration validation

- Release build passed (existing warnings remain).
- Allied/GDI art checks and selection checks passed (1814 concrete actors).
- New `test_soviet_building_art.py`: 3 tests passed, covering 23 actor variants,
  bib isolation, complete absence of legacy bib traits, seven independent bases
  and silo animation.
- The pre-edit resolved-rule fingerprints in `soviet-building-gameplay-baseline.json`
  verify that 21 existing actors retain all non-rendering traits, including
  footprints, selection, collision, docking, exits, weapons and economy.
- Existing supplied-art tests: 6 passed. PNG path audit: 88 assets, no errors.
- Protected Nod conyard hash unchanged.

In-game placement review remains necessary, especially for asymmetric refinery,
repair-depot and naval-yard artwork. Visible apron space does not add pathable
cells; gameplay footprints were not expanded or moved.

## Rear-bib alignment and selection frames

The GDI alignment approach now covers 18 supplied Soviet PNG buildings, plus
FACT.Nukular.Soviet through inheritance. Reviewed visible rear ground contacts,
not roof edges or the PNG canvas center, determine the native body offsets.
Cranes, fences, the runway and separate radar equipment are included in review.
The initial alignment pass retained existing bib registrations. Subsequently,
Tesla Coil was changed to the existing 1x1 bib (128x64), without resizing its art.
Its ground contacts are centered by Offset `2,-3` in all four body states; the
coil and its full-art selection envelope may overhang the smaller foundation.
[Current Tesla 1x1 preview](tesla-1x1-preview.png).

- Authoritative offsets: `sequences/soviet-building-placement.yaml`.
- Visual boxes/heights: `rules/soviet/building-alignment.yaml`.
- Contact coordinates, source hashes and calibration: `tools/soviet-building-alignment.json`.
- [Preview with resolved engine selection outlines](soviet-building-alignment-preview.png).
- Engine geometry and before/after validation: `soviet-building-alignment-geometry.json`
  and `soviet-building-alignment-validation.json`.

These selection diamonds are UI envelopes, not inferred collision footprints.
The complete alpha silhouette is enclosed, including roof equipment and Tesla
arcs. Missile-silo placement and selection cover the union of ALL SEVEN opening
frames: the doors spread sideways, so using idle alone is insufficient. The same
offset applies to idle, active, open and damaged states. Frame counts, source PNGs
and animation timing are untouched.

Helipad (legacy SHP), Sentry/TTUR/SAM graphics and the Blender turret study remain
unchanged. No source PNG was resized, recolored or otherwise rewritten. Paths,
occupied cells, mouse-interaction bounds, exits, docking, powers and weapon logic
stay unchanged; the naval-yard image preview is not a live water/docking test.

Eight actors had duplicate unnamed `Inherits` keys. They now have distinct tags
in exactly their original order so the new visual override file can merge safely.
Resolved before/after comparisons confirm no gameplay changes from this repair.

### Reproduction and checks

Export the actors listed in `tools/calibrate_soviet_building_alignment.py` with
`--export-allied-selection-geometry ACTOR ...`, then run:

```sh
python tools/calibrate_soviet_building_alignment.py docs/modular/soviet-building-alignment-geometry.json --propose tools/soviet-building-alignment.json --emit
python tools/calibrate_soviet_building_alignment.py docs/modular/soviet-building-alignment-geometry.json --preview docs/modular/soviet-building-alignment-preview.png
python -B -m unittest discover -s tools -p 'test_soviet_building_alignment.py'
python -B -m unittest discover -s tools -p 'test_soviet_building_art.py'
```

Re-export geometry after changing offsets or boxes before making a new preview.
Review contact coordinates again when supplied art changes; do not modify source
art merely to satisfy a hash/geometry test.

The five alignment tests cover all 18 buildings and all seven silo frames. The
three resolved-rule tests cover 23 Soviet variants. Their gameplay-only baseline
was derived from PRE-CHANGE resolved rules after checking the legacy baseline;
only `DecorationBounds`/`DecorationHeight` are excluded from selection hashing.
The old baseline remains preserved. The initial (pre-Tesla-1x1) 26-actor comparison recorded only
19 selection-decoration traits changed (18 buildings plus Nukular); original bib
registrations and mouse bounds are identical. All 550 pre-existing PNG hashes and
the previously exported GDI/Allied/Nod reference geometry remain unchanged.

The comparison above records the initial alignment checkpoint, not subsequent
user asset changes. The Tesla 1x1 correction also changes its decorative bib;
current alignment and resolved-rule tests pass with unchanged gameplay hashes.
Live-game visual review of the latest placement is pending. No running game was
stopped or restarted.

### Sentry on the disposable test map

`prepare-test-game-map.ps1` reuses the removed storage-silo slot Actor27 for the
independent `sentry.soviet` actor at `140,-64`, owned by Multi0. It is directly screen-above
HPAD.Soviet (Actor32 at `144,-60`): both have x-y=204 and the turret's x+y is eight
cells smaller. The original map archive remains unchanged. `test-gebaeude.cmd`
applies this placement when creating the next disposable test copy; no running
session is restarted automatically.

`SENTRY.SOVIET` has its own definition in `rules/soviet/building-art.yaml` and
uses `ftur.soviet.v13`: 32 native Blender
renders at 120x120 per frame, displayed at Scale 1. The base uses a separate
74x43 nearest-sampled derivative; its original PNG remains untouched. This replaces
the previous ingame Scale 0.5 trial. The 128x64 1x1 bib remains; height is unrestricted.
The rotation order is corrected to OpenRA's counterclockwise facings, without
mirroring any artwork. It starts facing SW (384) to
show the reference view. Prototype-only TurnSpeed 32 (instead of 1000) and 32
quantized facings make intermediate directions visible. Its selection decoration fits the
new visual envelope; mouse bounds and occupied cells stay unchanged. The old
Flame Tower weapon and muzzle spawn offset remain for this visual trial; the
tooltip identifies it as `Sentry V13 (art test)`. It inherits FTUR as a temporary
weapon/gameplay template but has no Buildable entry yet. The map no longer
overrides FTUR; its rules are identical to the regular Flame Tower definition.
A dedicated Sentry weapon remains pending. A separate real VXL/HVA alternative
is now registered as `SENTRY.SOVIET.VOXEL` at `142,-66`, directly screen-right of
the sprite Sentry. It keeps the PNG base, replaces only the turret with a voxel
renderer, and uses a source-color palette plus runtime normal lighting.
[Export, validation and limitations](../../art-source/soviet-sentry/voxel-01/README.md).
The engine file readers and actor dependency checks pass; live GPU lighting,
attachment and depth layering still need review.
The Sentry base is now centered on the bib using its two outer ground-contact
pixels: base Offset `0,-3`, turret Offset `-4,-24`. Both moved together (8px left,
2px down); the rotation attachment and native size are unchanged.

[32-facing assembly preview](../../art-source/soviet-sentry/game-v13-32-native/game-assembly-preview-2x.png),
[rotation animation](../../art-source/soviet-sentry/game-v13-32-native/rotation-preview.gif)
and [export/binding details](../../art-source/soviet-sentry/game-v13-32-native/README.md).
The preview is not an in-game capture. Restart via `test-gebaeude.cmd` to load it.

FTUR/TTUR still use their pre-prototype Sentry sheet, including inside this test map.
Their root sprite-sheet sidecar had been removed while a byte-identical sheet and
its nine-frame metadata remained under `rotational/`; the legacy bindings point
there. No original image was rewritten and the deleted root sidecar was not
restored. Tests check legacy bindings, standalone Sentry trait dependencies, and that FTUR
is not overridden by the map.
