# Plains / Mountain Valleys: starting economy and oil access

Follow-up: Plains now has a second owned expansion and additional contested patches; see [the current map-review implementation and evidence](map-review-status.md). The earlier field counts below describe the original starting-economy pass.

Built, tested and **installed locally in Game**, after confirming no user game was running. Applies to newly generated maps, not archived packages, handcrafted layouts, replays or the live tournament controller. No building footprint, unit statistic, resource value or oil payout rate was changed. This is a local balance change, not a game release.

## Reproduced problems

The old Plains starting circles and distances used staggered MPos coordinates as if both axes had the same physical length. Equal-looking placements therefore produced very different native access. Mountain Valleys scattered its patches randomly without guaranteeing a starting field. Both presets placed oil independently of fair access from starts.

Baseline and revised audits use native cardinal ground paths with height-step constraints, not screen distance. Example eight-player Strategic Patches cases:

| Preset / seed | Old first-resource steps | Revised first-resource steps | Old oil-anchor steps | Revised oil-anchor steps |
|---|---:|---:|---:|---:|
| Plains / 42 | 11–22 | 11–13 | 12–99 | 14–16 |
| Plains / 43 | 11–22 | 11–13 | 23–78 | 14–16 |
| Mountain Valleys / 42 | 18–58 | 25–27 | 36–97 | 31–33 |
| Mountain Valleys / 43 | 15–32 | 17–19 | 42–127 | 24–26 |

This is equalization, not a promise that every previously favored start becomes faster. The audit's arbitrary 24-step window is diagnostic, not a pass/fail boundary: all eight Mountain Valleys starts in seed 42 now have their complete owned fields just beyond it instead of some starts waiting 58 steps for any resource.

## Policy

- **Patches:** every real start receives the same cell budget within its map. Fields are connected native-cell shapes that adapt to available low ground, not truncated circles losing cells against cliffs. Each field cell must be reachable and closer to its owner by at least three planning steps. Group placement uses a common access band; failure rejects/replans the layout, never silently omits a player.
- Plains retains its seeded radius policy: initial budgets are 81/113/149 cells. Its equal-sized expansion group is also placed using native access, with a minimum 26-step approach rather than accidentally appearing beside some starts.
- Mountain Valleys receives 81 cells per start. This replaces half of the old random patches; the remaining random expansion patches stay beyond the immediate starting-access area. Roofs, ramps and strategic reservations remain resource-free.
- Non-finite Patches gives every initial field a regenerator. Mountain Valleys no longer assigns only some starts regeneration through its old half-player random-generator count. Finite mode still has none.
- **Oil:** Sparse means one owned derrick per real start; Dense means two. A whole access-balanced group is selected before it is applied. Full 2x2 native footprints and approaches must be free of terrain/actor obstructions. This deliberately changes oil counts from the old arbitrary map-wide singleton/cluster totals; it is not economy-neutral. In Glitter mode only the four foundation cells may be cleared of resources.
- Irregular mountain terrain does not promise exact rotational geometry. The invariant here is equal allocation with comparable ground access, unlike Two Sides' exact center-reflected oil pairs.
- Explicit Plains `resources`, `finite` and `tech` CLI options now take precedence over preset defaults. None/Glitter options are not silently converted to Patches. Glitter terrain obstacles are planned before scattering resources on Plains, avoiding the old all-candidates-occupied failure. Mountain Valleys' separate Glitter regenerator policy is unchanged.
- Initial footprints and nearby core building areas are preserved. The audited problems were mainly supply/access, not missing room to place the construction yard. Compact 11-player mountain homes still have smaller cores (175–195 free native cells within 12 steps in seed 42). Blindly increasing the home radius failed compact-layout terrain constraints and was rejected. The regression floor is 160 cells; this does not certify spacious later base expansion or close every cramped-base complaint.

## Implementation and validation

`BalancedStartingEconomy` plans equal budgets and owned oil; `StartBalanceMetrics` reports native conservative path distances. Starting-field metadata survives plan cloning and is checked after terrain/objective processing, including preserved resources, connectivity and approach length. Plains/Mountain debug resource-distance reporting now labels and uses native cardinal paths.

- `--start-balance-audit OUTPUT.json [preset] [players]`: inspect access, nearby free build area and owned starting budgets.
- `--start-balance-test`: 36 complete layouts covering all supported player counts 2–12 and 16 in both presets, plus Tactical/Operational None/Glitter/Patches controls, explicit CLI option precedence, finite mode, Dense/None oil, construction-core protection, clone/field-loss rejection and deterministic regeneration. `options` runs only option controls.
- `--symmetric-oil-test`: separate existing Two Sides regression; its default placement behavior is unchanged.
- Native four-player Strategic exports for both presets: real harvesters reach resources; engineers/jeeps/medium tanks traverse the map; all four oils are reachable from all starts; all fourteen construction-yard types fit near all four starts. Native Mountain Valleys harvester routes were 17/18/18/19 cells. A further 11-player native run passed all fourteen construction-yard types at all eleven starts, all oil approaches and actual movement; its harvester routes were 16–19 cells.
- The review-only fixture disables Autobattler/ArmyFollower, which otherwise intentionally replaces manually queued vehicle orders in Strategic/Operational mode. Production rules are unchanged. An initial test caught that autopilot override, not an impassable route; no path assertion was relaxed to hide it.
- `HandcraftedRuntimeProbe.CheckHarvestRoutes` defaults off. When enabled for these fixtures, it checks candidate resource routes using native pathfinding and a safe distance lower bound; it does not mistake a close resource behind a cliff for the nearest road-accessible deposit.
- Native overviews were inspected. Evidence is under `art-source/map-balance/validation/`.

Reproduce: export a fresh procedural map using `--debug-map-generator ... export-map=true`; run `python tools/prepare_start_balance_probe.py INPUT.oramap REVIEW.oramap`; use `tools/test-handcrafted-map-runtime.ps1 -WaitForMovement` with an isolated engine directory. Never install probe packages in normal map folders.

## Compatibility and remaining scope

New generation of an old seed can yield different economy and terrain attempts; existing downloaded/archived maps remain authoritative and are not regenerated. Handcrafted maps and the protected Nod construction-yard image were not edited. No tournament bot or release-branch update is included. Projectile/cliff behavior, building occlusion, faction silhouettes/colors and handcrafted coast touch-ups remain separate work.
