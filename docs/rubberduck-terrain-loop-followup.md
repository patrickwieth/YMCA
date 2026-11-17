# Terrain acceptance loop — fixes and final regression

The requested legacy-contact, real naval-route/dock, height-gameplay and large-map
performance follow-up is complete in the test matrix below. This is not a claim
that every unused artist template has been mapped, every seed is competitively
balanced, or all hardware sustains constant 60 FPS. Evidence root:
`%TEMP%/ymca-terrain-loop`. Bridges were excluded from those runs. Supplied sprites
have since enabled the separate [six-part bridge implementation](rubberduck-bridges.md);
that later feature is not retroactively covered by this earlier soak.

## Measured rendering work

- `profile-baseline`: first camera-change window reached 512.22 ms; native cliff
  cold builds alone took 358.24 ms across 269 faces.
- Clipping and its source sheets are now prepared during map loading. The measured
  192x192 preparation took 3408 ms. Editor invalidation remains lazy/revision-aware.
- `profile-prepared`: corresponding camera-change maximum 95.33 ms, no cold builds.
- Immutable renderables were then reused: `profile-reuse` completed 15000 ticks in
  600.25 seconds. Post-initial-window worst frame 73.30 ms, GC2 count 25 at finish
  versus 57 in the earlier accepted run. These are different runs, not a universal
  speed/memory guarantee. Initial exploration still caused a 279.58 ms sample.
- Later changes group each face's strips, evaluating lighting and depth sorting
  once per face. Four actual cliff-image rectangles (11200 pixels total) compare
  exactly across this change. Whole-image comparison also contains animated/random
  resource sprites and a deliberate experimental talus change, so it is not zero.
- A .NET sampled-thread trace in `bot-vision-cache/cpu.nettrace` identified palette
  pulse copying and repeated lighting work. Pulse copying now uses bulk ARGB copies
  while preserving index zero and the configured shadow entry. The palette utility
  checks 39936 exact ARGB comparisons and cloak palette append/replacement behavior.
- Physical shroud ranges now have a bounded map-local cache (4096 entries and
  1048576 retained projected cells, whichever limit is reached first). The height
  utility checks 4200 small and 600 large-range origins, coverage equality, height invalidation and
  retained-source immutability. A real Challenger fog/lifecycle session passed in
  `vision-cache`; all six representative units were rerun after the final changes.

## Actual bots, not a layout-only test

`Launch.Map` starts a **Local** server. The capture helper's historical
`PlayerType.*`/`PlayerFaction.*` arguments are not consumed by this engine; they
must not be cited as proof of faction or bot configuration.

Only a Local server whose map explicitly contains `TerrainSoakProbe` with
`BotCount > 0` now adds requested real `normal` bot clients and spectates the human
connection. Ordinary skirmish and multiplayer initialization are unchanged.
Observer rendering does not change bot shroud rules. Diagnostics fix simulation
seed 43; bot-local randomness still prevents claiming identical matches solely
from this seed.

- `bot-smoke`: initial seven-bot plus idle-human smoke test.
- `bot-observer-smoke`: verified eight real bots, true observer, 1000 ticks.
- `bot-long`: intentionally interrupted, **not** a completed 45000-tick test.
  It demonstrated harvesting, unit/building kills, but warm simulation spikes
  reached 794.34 ms per frame (recent logic 77.76 ms at tick 15000).
- `bot-profile` and `bot-vision-cache`: each completed 10000 ticks with actual combat.
  They are diagnostic/profiling runs, not final frame-pacing certification.
  Per-path metrics and simulation performance logging distinguish logic from
  rendering. Trace collection/logging overhead must not be ignored.

Simulation logging settings persist in OpenRA's settings file. The capture helper
now explicitly sets logging false unless `-SimulationPerf` is requested, rather
than accidentally carrying profiler overhead into later acceptance runs. The clean
long-match rerun below explicitly disables this logging.

`bot-render-final` is also an interrupted baseline, despite its directory name:
range searches accumulated over seven seconds in a 500-tick window and frame spikes
still exceeded 500 ms. A boundary-only target reduction passed mathematical tests
but regressed actual performance; it was removed and archived under
`rejected-boundary`. `bot-boundary` is not accepted evidence.

The current `SafeRangePathSearch` instead searches forward toward the complete
range target set using the engine's public movement graph, costs and layers, with
unsafe diagonals filtered directly. It preserves the engine's 125% heuristic bound.
A 2048-expansion budget falls back to the established finder; budget exhaustion is
never reported as unreachable. One thousand weighted-graph tests check reachability,
path cost, source handling, zero-cost layer edges and budget fallback. The final `bot-forward-range` run completed **45000 ticks in 1800.70 seconds**
(30 minutes of real eight-bot combat on the 192x192 map). It recorded 90 sample
windows and 166976 rendered frames, all focused. Combat advanced in 88 of the 89
inter-window comparisons and produced 78 additional kills in the final 2000 ticks;
this was not an idle simulation after an early game-over.

- Eight real bots: 883224 earned resources/cash, 1674 unit kills and 85 building kills.
- Window frame medians 5.45–19.52 ms; p95 12.80–40.36 ms; p99 21.45–51.00 ms.
- Largest frame 266.52 ms in the first window; largest later frame 159.17 ms.
  This is not a promise of zero GC/driver hitches or constant 60 FPS at every zoom.
- Recent logic averages 6.01–18.14 ms; the simulation maintained normal real-time
  speed instead of the previous sustained range-search stalls.
- Managed memory 492–826 MB, final 644 MB; private memory peak 2.60 GB, final 2.47 GB.
  No monotonic growth/OOM appeared in this session; unbounded-session leak freedom
  is not implied.
- All terrain, palette and shroud fixes remained enabled. Simulation performance
  logging was explicitly disabled; no profiler was attached to this final run.

## Final regression results

- `final-combat-{challenger_tank,mtnk,ltnk,t-34,chbattle,seek}`:
  **96 combat, 96 sight and six source-removal/lifecycle sessions PASS**.
- `final-plateau-normal`, `final-plateau-shaded`, `final-operational`,
  `final-strategic`: each **32 movement legs, 16 blocked-foot checks and a real
  height-four MCV deployment PASS**. This is movement/deployment regression,
  not a new complete certification of every mode-specific victory/XP rule.
- `final-naval-basins` plus the four corrected 4P42 water-preset sessions and
  `final-naval8-{archipelago,migration}`: **36 dock placements, 72 real productions,
  144 actual legs PASS**. The two 8P43 sessions use the normal tileset; maximum
  reciprocal timing differences were 1.59% and 0.75% respectively.
- Final contour re-audit: **72 exports**, zero accidental legacy decorations;
  180 valid / 543 rejected corrupt contours, five original fixtures, 52 protected
  feet and export/editor parity PASS.
- `final-legacy-west` parks the Challenger directly beside the southwest abutment;
  `final-legacy-east-shaded-editor` repeats the six editor restoration checks in
  the shaded southeast case. No original saved terrain was rewritten.
- Release build and whitespace checks pass, with existing unrelated warnings.
  All 33 immutable source hashes still match the source catalog. All seven naval
  fixture terrain binaries match their source maps byte-for-byte. Temporary
  `terrain-loop-*.oramap` packages were removed from the mod map directory.

## Naval runtime

`--rubberduck-naval-playtest MAP OUTPUT [BODY_RANK=0]` uses unchanged production
terrain. It validates actual shipyard footprints and height-aware foot access,
then invokes normal production exits and drives one actual ship at a time in both
directions. DD and LST retain their original movement domains, including legitimate
landing-craft beach movement. Reciprocal differences are checked against a
predeclared tolerance of 10% or 25 ticks. This is not universal strategic fairness.

`naval-water-basins`: four shipyards, eight real productions, sixteen completed
legs. An initial fixture accidentally blocked a narrow basin with later shipyards;
selection now rejects docks that cover or disconnect earlier test ports. No
locomotor or production terrain was weakened to pass the test.

The first Archipelago runtime exposed a real movement bug: a DD's physical center
entered Clear terrain at 121,-50 during a diagonal coast turn. The corner-safe
pathfinder had been enabled only when maps contained elevation. It now applies to
flat Rubberduck coasts too; locomotor domains and strict physical-position checks
are unchanged. The corrected four-preset runtime batch completed in `naval-safe-archipelago`,
`naval-safe-migration`, `naval-safe-continents` and `naval-exits-two-sides`.
Together with Water Basins, this is 20 dock checks, 40 productions and 80 actual
legs. Maximum reciprocal differences range from 0.7% to 8.3% (the latter is a
five-tick difference on a short Continents leg). Later fixtures lock fastest game
speed for throughput and report simulation ticks, not wall seconds.

Two Sides exposed two fixture-placement defects: the first launch could be trapped
in a singleton behind its own yard; then normal exit selection could choose a
corner without a cardinal exit into the main sea. Selection now preserves the main
remaining component and requires every production corner to have an exit into it.
No production/locomotor rules were weakened. The final Water Basins rerun and both larger-layout travel checks also passed;
see the final regression totals above. `naval-final-runtime.tsv` consolidates
per-leg timings. The tests establish usable placement/egress and reciprocal
navigation, not equality of every optional shipyard choice or universal balance.

## Legacy blocked-foot rendering

`LegacyCliffAbutments` supplies the existing original rock-pile crop only at mixed
legacy/vertical continuing-wall sockets with unreserved diagonal endpoints. It
never adds actors to saved maps or changes collision/resources. Closed-family
actors and blocked mountain roofs are excluded. Ordinary convex native ends are
also excluded; an earlier overbroad selection produced unnecessary outcrops.

The pile's authored ground baseline is row 160 (opaque bounds rows 63–164), not
row 256. `legacy-talus/ingame.png` is the rejected floating candidate;
`grounded.png` exposed coarse actor-depth ordering; `foreground.png` is the
subsequent map-local test. `legacy-auto` checks the shared renderer with a real
Challenger beside the blocked foot. Source pixels are not warped or recolored.

The editor uses the same layer with terrain/resource/history invalidation.
`legacy-editor` and `legacy-east-editor` each pass six cache/transaction categories,
including exact restoration and save/reload without inserted decoration actors.
The ordinary legacy grow brush adds five more PASS categories, and
`editor-regression` passes ten ordinary stamp/removal/protection/history categories.
Actual normal/shaded southwest and normal southeast views include a real Challenger
that cannot enter the blocked foot. The east test parks directly beside it.
All four inspected legacy fixture terrain binaries remain byte-identical to the
original, SHA256 `e2d339d971d273c6dd905cc79a8d15148219bd28476f69a021c8866ed6c13eeb`.
All 72 modern closed exports re-audit with zero added legacy-foot decorations.
The final gameplay/render regression batch above also passed. The original brown reserved
terrain is not painted green. This treatment is an explicit rock abutment, not a
claim that old mixed contours have become the new closed family.
