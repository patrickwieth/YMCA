# China/Scrin building size and China foundations

User follow-up requests: Soviet Naval Yard needs water in the building test; China
should use the Soviet Naval Yard image and Soviet bibs; newly added China/Scrin
buildings should be twice their previous visual size. Implemented locally in
Game/modular. No source artwork overwritten, no commit/push/release.

## Changes

- Existing China/Scrin body, damaged, active, turret and building-detail sequence
  scales are multiplied by2, rather than setting all to a blanketScale2. Examples:
  Plasma Turret body0.5→1, turret0.4→0.8; Chinese Gatling2→4. UI icons, generic status
  overlays, bib graphics, existing generic construction and obsolete make animation
  are not doubled. Body detail overlays such as radar/smoke scale with their body.
- Turret attachment and armament local offsets scale by2 alongside the art;
  selection dimensions/heights scale correspondingly. Physical occupied cells,
  hit shapes, exits, docking, range/damage, production, prices and power stay intact.
- Chinese land buildings use native Soviet1×1/2×2/3×3/4×4 decorative bibs. Legacy
  `WithBuildingBib` is removed, not merely hidden. Bibs are actor-bound and hidden
  during construction, including mixed-faction test ownership.
- CHSYRD directly binds **spen.soviet**, the same image as SPEN, resolving to the
  authoritative `bits/soviet/buildings/shipyard.png` with the exact Soviet scale,
  anchor and placement. **This newly shared PNG yard is intentionally NOT doubled**:
  it retains the existing Soviet native size. Its selection decoration copies the
  Soviet yard registration. Ship production/faction requirements remain Chinese.
- The disposable test water generator now includes SPEN as well as CHSYRD and the
  existing Allied/GDI/Nod yards: five pools, with ship-exit borders. Original
  `testmap1.oramap` is unchanged; current `testmap1-debug.oramap` regenerated.

## Isolation and YAML merge compatibility

The building art override files are loaded after existing placement layers.
The original China/Scrin files had repeated bare `Inherits` keys that worked in
single definitions but failed when merging a later actor override. Their inheritance
keys are now uniquely named with the same parents/order. Two duplicate ordinary
traits were deduplicated preserving the **previously resolved** result: Chinese
Command Center BOTRADAR retained its existing `botowner` condition; REAC retained
its identical `anypower` prerequisite. No hidden radar/gameplay change was added.

NKOREA_BUNKER inherits the Chinese bunker actor. Its existing Soviet bib, supplied
art and selection must stay unchanged: remove the newly inherited China bib instead
of the now-absent legacy bib, and retain its old mouse bounds. Existing Soviet
baseline regression verifies preservation; do not update that baseline to hide a
change. Allied/GDI/Nod and Soviet shipyard images/scales are not doubled.

## Files / regeneration

- Frozen pre-change engine output: `actors-before.json`, `sequences-before.json`.
- `tools/art/prepare_china_scrin_scale.py` generates the explicit override files
  and `binding.json` from those snapshots; it does not compound scales on rerun.
- Runtime: `mods/ca/rules/china-scrin-building-art.yaml`,
  `mods/ca/sequences/china-scrin-building-scale.yaml`.
- Chinese and Scrin normal actors/variants inherit these visuals in game; this is
  not just a temporary map-only size change.

## Validation / remaining check

Release configuration build passed. `test_china_scrin_scale.py` checks every
scaled state, unchanged icons/construction, Chinese bib bindings, identical shared
yard registration and all resolved nonvisual gameplay traits against snapshots.
Existing Soviet building-baseline tests pass. Extended real-engine map checker
loads every new actor's body and China bib sequences, verifies both yards select
spen.soviet and stand on Water, and checks map bounds/non-overlapping footprints.
Building-test generator/source-preservation tests pass.

**Live visual review at2× remains pending**; numerical scale and footprint tests
are not proof of ideal appearance. Start `test-gebaeude.cmd` again to load the
current local rules and freshly generated water. No running game was stopped.
