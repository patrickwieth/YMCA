# Two Sides: bank assignment and shoreline feedback

Latest: [wider river/causeway, centered objective and ground joins](two-sides-wide-crossing.md).
That follow-up supersedes the layout measurements and captures below.

Reproduced using the player's Strategic four-player map, seed `1775378146`.
Changes are local and require regeneration; saved maps are not rewritten.

## Follow-up: native diagonal layout and smooth cliff runs

The latest layout follows one of the two native isometric axes (MPos slope +/-2).
Water is sampled in **CPos**, not just drawn along a diagonal MPos line: staggered
row parity otherwise reintroduces an alternating tooth at each shoreline cell.
Broad, low-frequency bends remain, while long stretches use straight authored
cliff faces rather than alternating inner/outer corner pieces. The previously
added authored diagonal beach selection remains active. No artwork was redrawn.
The artificial periodic width bumps on the causeway were removed.

Starts are spaced along each native bank. Translate the formation so the first
slot is on the river normal, then order center-out: the usual human/opponent pair
is exactly diagonal in screen space, on separate banks. Resource-patch searches
follow the bank normal instead of assuming north/south banks. Failed complete
resource layouts retry, without erasing fields. Resources=None creates no mines.

Coast selection now checks the complete possible end-piece footprint (two water
rows and up to two cells along either tangent), before collecting a contour.
This keeps both long cliff runs without ending a rock piece on nearby land or
protected access. Existing collision/connectivity validation remains in force.

Latest evidence supersedes the earlier jagged-cliff captures below:

- Release build and 36 twice-generated cases pass, now also asserting native
  diagonal separation of the default opponents (players 2–12/16, three seeds).
- Four-player user seed 1775378146: two accepted cliff runs, zero rejected
  coast candidates, 63-cell Strategic route, reported nearest-resource distances
  min/average/max=11.7 cells across the four starts. No all-seed balance claim.
- Actual client: four team/spawn assertions and twelve autonomous checkpoint
  visits, complete at tick 1525. Same accelerated/no-victory diagnostic as below.
- New-map naval fixture: four dock placements, eight productions and sixteen
  reciprocal legs pass with unchanged locomotors and diagnostic-only suppression
  of autonomous ship orders.
- `%TEMP%/ymca-two-sides-diagonal/{end-checked,accepted-play,naval}` and
  `%TEMP%/ymca-two-sides-diagonal-*.log` contain the current evidence.

[Current native-diagonal client capture](images/two-sides-diagonal.png).
The earlier `contours` and `native` iterations are not final acceptance: the latter
still rejected one end piece. `end-checked`/`accepted-play` include its correction.

## Starts and objectives

`TwoSidesGenerator` already ordered starts by river bank. The generic Strategic
projection sort subsequently split them along the river instead. Preserve the
bank order for this preset. Export two locked teams and matching locked spawns
for Two Sides in Tactical, Operational and Strategic; the existing fresh-lobby
opponent selection then picks the other bank (human spawn 1, opponent spawn 3
in the four-player case).

Strategic routing now uses native CPos neighbors on flat maps too. Two Sides
objectives use a whole-chain selection over fitting positions, rather than a
greedy choice that exhausts later pads. Check the actual FCOM 2x3 footprint,
including its passable approach row, against existing terrain and economy.
The crossing is already exported as sand: keep it sand. Do not turn surrounding
water into circular grass pads or delete resources. Retry a complete deterministic
layout if the requested chain cannot fit. Other presets retain their pad policy.

## Shore geometry and artwork

- Classify water contacts in the native isometric grid, not rectangular MPos
  adjacency. This closes gaps in the planned defensive shore contours.
- Run the existing conservative, area-neutral coast smoother before marking
  shores and placing economy. Homes/crossings are protected. The reported seed
  accepts two paired swaps; this is not arbitrary water filling to hide art joins.
- For Two Sides, check prospective face/end protection before collecting rock
  contours. Previously a protected endpoint rejected an entire long candidate,
  leaving only short disconnected pieces. Require at least six bank cells and
  retain whole-contour footprint/connectivity checks. The reported seed now
  exports two contiguous rock coasts, one per defensive bank, instead of five
  short fragments. Landing protection, height-zero water and collision remain.
- The shared runtime/editor shore compositor now uses one original diagonal
  half-cell frame for exactly two adjacent water edges. Safe even frames are
  46/44/40/42 for south/west/north/east water. Never intersect opposing wedges,
  and never use the odd sand variants that erased cell centers. Tiny islands,
  narrow spits and opposite-edge cases retain the earlier full-cell composition.
- No source artwork or installed atlas pixels changed. Regenerated atlas SHA256
  remains `5a7f6c3b0a11fb4645435f8463a630c0ffe79ce275589ba6567bea9e3d048437`.

The earlier close-up below shows the native rock family's repeated, stepped
silhouette before the native-diagonal follow-up. Those captures are historical,
not the final appearance. Neither stage establishes universal visual acceptance
or arbitrary smoothly curved cliff geometry. No collision was
hidden or relaxed to make the screenshot look smoother.

## Checks and actual client evidence

- Release build and whitespace check pass.
- `--two-sides-test`: 36 cases, players 2–12/16 and seeds 42/43/1775378146;
  repeated generation, native disconnected dry banks, preserved spawn order,
  unchanged terrain/economy across objective placement, fitting footprints;
  16 diagonal orientation/variant checks and narrow-land controls.
- Exported four-player user seed verified for locked teams/spawns in all modes.
- Actual Two Sides UI probe: chooser/back/Customize and opposing default bot pass.
- User seed autonomous runtime: four occupied team/spawn assertions, twelve
  checkpoint visits in both directions, complete at tick 3351. Accelerated
  diagnostic waves and disabled victory are not a full-match balance test.
- 1880 authored-shore donor/coverage/center-opacity compositions pass.
- Normal client shore fixture: five land and five water movement assertions,
  forbidden-domain rejection, and 95 mixed-water joins pass.
- Shaded real editor: ordinary brush, exact Undo/Redo, save/reload, and bounded
  920-sprite/two-sheet cache pass.
- User seed naval fixture: four actual docks, eight productions, sixteen
  reciprocal travel legs pass. This fixture disables Autobattler orders because
  otherwise Strategic waves override its explicit ship routing. The earlier
  non-isolated naval run timed out and is not accepted evidence. Production
  Strategic behavior was not changed to satisfy the probe.
- Mountain Valleys: 24 twice-generated front cases pass. Additional Strategic
  Hill Fortress, Arabia and Water Basins four-player seed-42 exports pass.

Evidence: `%TEMP%/ymca-two-sides-feedback/` (`final`, `final-play`,
`naval-isolated`, `shore-normal-complete`, `shore-editor-shaded`, `shore-art`),
`%TEMP%/ymca-ui-probes/two-sides-feedback/`, and
`%TEMP%/ymca-two-sides-*.log`. Each runtime directory contains a results pointer.
The first normal shore capture stopped at the first screenshot; use
`shore-normal-complete`, which waits for all ten movement assertions.

Review: [overview](images/two-sides-feedback-overview.png),
[cliff close-up](images/two-sides-feedback-cliffs.jpg),
[lobby](images/two-sides-feedback-lobby.png).
