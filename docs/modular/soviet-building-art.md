# Soviet building separation and foundations

Local work on `Game / modular`; included in the user-requested building-art checkpoint. No push.

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
- 2x2: sentry/tesla turrets, Tesla Coil and SAM/flak base.
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

## Validation

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
