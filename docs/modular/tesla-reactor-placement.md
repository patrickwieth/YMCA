# Tesla Reactor placement follow-up

Local only, after v0.97.5. No commit, push or release.

TPWR was missing from the building-test display. The disposable map preparer now
adds `TestTeslaReactor` at `122,-58` (Russian faction, owned by Multi0), screen-above
the Soviet nuclear plant. Existing power plants and the authoritative map are retained.

Native review confirmed that TPWR still used the body/selection registration for
its former 4x4 bib despite now having a 3x3 bib. Re-running the existing calibration
with the current bib gives body Offset `1,-25` instead of `1,-57`: 32 pixels down.
Both idle and damaged-idle use this registration. Selection bounds follow the same
shift: `3077,3077,0,-634`, unchanged height133. The TPWR entries in calibration data
and geometry fixture were updated accordingly; other entries remain historical.

Original PNG, footprint `xx= xx= ===`, dimensions3x3, local center `0,724,0`, power,
cost, weapons and abilities are unchanged.

Validation:
- Release build passes; isolated native detail (100%) and context (50%) renders
  reviewed after the correction. Machinery now sits within the rear 2x2 portion
  of its bib, with clear front paving and an enclosing selection frame.
- Evidence: `art-source/tesla-reactor-placement/{before,after}-{detail,context}.png`.
- `test_building_testmap_factions.py` passes, including exactly one TPWR and source
  map immutability. Protected Nod conyard checksum passes.
- Focused `test_tesla_current_three_cell_bib` verifies reproducibility, native source
  checksum, selection containment and both production sequence offsets.
- The broad historical Soviet calibration suite remains failing on unrelated old
  asset hashes/geometry (APWR, FIX, Iron Curtain, missile silo); it is not claimed green.

`BuildingPlacementReviewProbe` is diagnostic-only, requires environment opt-in
`YMCA_BUILDING_REVIEW_PROBE=1`, and is attached only to an isolated review map.
It is not added to the normal test launcher or runtime world rules.

Restart `test-gebaeude.cmd` to see the added reactor and correction. No running
user game or server was stopped.
