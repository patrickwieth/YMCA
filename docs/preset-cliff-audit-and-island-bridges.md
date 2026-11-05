# All-preset cliff audit and Archipelago bridges

Subsequent changes: [Migration now has open coasts and small oil islands; Arabia is named Plains](migration-oil-islands-and-plains-name.md). [Hill Fortress now has raised team bases and a low valley](hill-fortress-team-plateaus.md). The audit below records the earlier baseline; Continents/Archipelago coastal fragmentation and Mountain Valleys' old expansion walls remain open.

## Audit scope and findings

36 physical exports: all nine presets, Tactical, 4/8 players, seeds 42/43, automatic sizes. Each preset also received a real client close-up (4 players, seed 42). This is a scoped audit, not an all-seed or all-mode visual certification.

| Preset | Finding |
| --- | --- |
| Continents | Fragmented flat rock coasts confirmed. 12–19 coast components, only 38–57 bank cells in total. Old face/corner assembly remains. |
| Archipelago | Same unresolved coast pipeline; 9–23 components with 42–109 bank cells. Floating-looking caps/repeated broken corners visible in game. |
| Migration | Same unresolved coast pipeline; 7–16 components with 21–53 bank cells. Particularly short isolated fragments. |
| Mountain Valleys | Mixed rendering: closed home/island plateaus, but expansion rims still export 430–1066 old projected `mountainwall` faces per tested map. Angular strips and disconnected-looking corners confirmed in game. |
| Hill Fortress | Closed contours; no projected mountain walls in these exports. No corresponding fragmentation found in the reviewed close-up. |
| Battle Nexus | Closed contours; no projected mountain walls in these exports. No corresponding fragmentation found in the reviewed close-up. |
| Arabia | Closed contours; no projected mountain walls in these exports. No corresponding fragmentation found in the reviewed close-up. |
| Two Sides | The accepted continuous, boundary-reaching coast pipeline: exactly two banks. Kept unchanged. |
| Water Basins | No cliff banks or cliff actors. Kept cliff-free. |

The closed portions of all 16 mountain-preset exports passed the unique-socket audit. This does **not** certify Mountain Valleys' additional projected expansion walls. The audit initially included cliff-free/island maps, which the closed-contour command correctly rejects as having no closed actors; it was rerun on the appropriate subset without weakening validation.

The earlier phrase “shared cliff update” was too broad. Shared material variation, closed mountain topology, and flat coastal topology are separate changes. The three island-coast presets and the remaining Mountain Valleys expansion walls need further topology work. This patch does not hide those walls or claim to have repaired them.

## Archipelago: bridges replace permanent shallows

`IslandsGenerator` now asks the existing `RubberduckBridgePlanner` for same-team crossings instead of painting arbitrary shallow-water lines. The planner's original Continents behavior is unchanged when no team ownership is supplied.

- Endpoint ownership uses actual native-cell land components, not distance to island centers.
- Only existing dry banks with protected economy/build clearance respected are eligible. No sea filling or resource removal.
- Deterministic shortest-first, non-overlapping forest; no cross-team links or ground cycles.
- Equal bridge counts per planned team. If every team has at least two players, at least one crossing per team must fit or the complete layout is retried. If a team is a singleton, no team receives a bridge advantage, matching the previous comparable-team policy.
- Not every island must be connected: incompatible islands remain transport destinations. Equal bridge counts are not a route/balance guarantee.
- A clear 3-by-5 water cross-section around each span midpoint is required. The first runtime test exposed a crossing beside a third island whose naval destination was dry land; this added rejection fixes the actual clearance problem rather than changing water or weakening the probe.
- The plan still uses `ShallowWater` internally together with `DestructibleBridge` for ground connectivity. Export writes **real water**, with destructible bridge actors above it; there are no permanent sand causeways.
- As with existing Continents bridges, intact spans block naval movement across the deck line. After destruction, water and naval entry return. Protected launch shores remain reserved.
- The team-connectivity validator now follows native cell axes, so it can actually follow the bridge cells rather than the staggered debug-grid axes.

New generation is required. Existing saved Archipelago maps are not rewritten. Geometry/retry outcomes and neutral economy placement can differ for the same seed.

## Evidence and checks

Evidence root: `%TEMP%/ymca-all-preset-audit`.

- `baseline.tsv`, `baseline/`: per-export actor families, bank cells and coast counts; nine `<preset>.png` client close-ups.
- `--archipelago-bridge-test OUTPUT`: **31 twice-generated physical exports**, Tactical/Operational, 2/3/4/6/8/12/16 players, seeds 42/43, plus one-team/three-team/FFA checks. Verifies no permanent shallows, native enemy isolation, deterministic terrain/economy/bridges, equal counts, dry protected ends, clear water cross-sections and atomic protected-bank rejection.
- `--rubberduck-island-economy-test`: 12 twice-generated cases retaining complete equal-sized dry home resource patches.
- `--rubberduck-bridge-plan-test`: existing Continents pair/ring regressions passed; `--rubberduck-bridge-bank-test`: 48 bank masks passed.
- Real 4P42 normal and 8P43 shaded clients completed **2 and 6 spans** respectively: tank/infantry crossing in both directions, draw ordering, weapon-driven collapse, stranded-unit removal and ship passage after collapse. Result pointers: `bridge-play-final/` and `bridge-play-shaded-final/`. The first 8P capture stopped too early for the long spans; the rerun reached `BRIDGE COMPLETE spans=6`.
- Real editor on the generated 4P map: preview/runtime parity, bank-cache invalidation, exact Undo/Redo, terrain/resources/actors/rules save/reload passed (`bridge-editor/`).
- Full naval run on the generated 4P42 map: **4 real dock placements, 8 ship productions, 16 reciprocal travel legs** (`dd` and `lst`) with bridges intact; all completed. The initial 300-second capture was too short and was not counted as a pass; the longer rerun reached `NAVAL COMPLETE`. Result pointer: `naval/runtime-results-path.txt`.
- Release installed after confirming no client was running: 0 errors, 3 known warnings. Modular tests: 180/180; `git diff --check` passed. Only this audit's diagnostic fixtures were moved out of the playable map list.
- `preservation.txt`: complete `map.bin` and `map.yaml` byte-identical for regenerated Continents, Two Sides and Water Basins 4P42 baselines.
- `closed-contours.log`: 16 exported closed graphs plus 180 general contours, 543 rejected corrupt contours, five fixtures and 52 protected feet.

No new multiplayer, full-seed reliability, strategic fairness or performance acceptance is claimed. No source artwork was changed. The cliff problems listed above remain open.
