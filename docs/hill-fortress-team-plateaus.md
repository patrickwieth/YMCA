# Hill Fortress: raised team bases and a low battle valley

**Follow-up:** [open map edges and recessed five-wide ramps](hill-fortress-open-edges.md) supersede the initial outer margin and three-wide ramp description below.

New generation replaces the two blocked mountain ridges with **two large, walkable height-four team plateaus**. All player starts, their nine-cell build-clearance disks and home resources are on the raised roofs. The central battle area stays at height zero and retains its centered dirt material. Each team has one three-cell-wide, four-level ramp into the valley.

A narrow six-cell outer planning margin keeps the raised playable areas inside projection bounds and leaves room for visible cliff feet. This is not a raised map-border implementation. Only the committed roof is marked as Home. No flat player base is hidden behind a decorative ridge.

Implementation:
- `HillFortressPlateauPlanner` uses the shared `MountainPlateauPlanner.AddHome`, native closed contours, reserved feet and established ramp geometry. Two native-axis orientations are supported.
- Start placement requires the entire build disk to lie on one team's flat, walkable roof. Resource fields and generator footprints must also fit above; patches cannot overwrite build clearance. Failed complete layouts retry rather than losing fields or obstructing ramps.
- Validation checks height-aware reachability of all roofs and both teams, a reachable low contested center, and team separation when the ramp lanes are blocked.
- The renderer/editor is unchanged. Sloped ramp retaining faces remain legitimate; tests reject full-height projected fallback walls and require every retained slope face to neighbor an actual ramp.

## Validation

Evidence: `%TEMP%/ymca-hill-team-plateaus`.

- `--shared-mountain-topology-test`: 24 twice-generated Hill Fortress exports across Tactical/Operational/Strategic, 2/4/8/16 players, seeds 42/120999725, plus 8 Battle Nexus regressions. Updated assertions distinguish walkable Hill roofs from blocked Nexus rocks.
- All 32 exported closed graphs passed unique-socket validation; general suite: 180 contours, 543 corrupt rejections, five fixtures, 52 protected feet.
- Six additional Operational exports: 2/3/4/8/12/16 players, seed 43. Correction: these did not validate patches; the CLI's then-existing preset override selected Glitter despite the explicit patch argument. Six Strategic exports also cover 5/6/7/9/10/11 players; the centered-dirt regression passed its six cases.
- Real Strategic 4P42: autonomous tanks from all four homes descended into the valley and completed all 12 checkpoint visits; final tick 1807. This is a connectivity test, not competitive travel-time parity.
- Real shaded 4P120999725: infantry and heavy tanks ascended and descended both ramps through all four levels; blocked cliff feet reject entry/pathfinding. The home MCV deployed successfully on the roof (`ramps/runtime-results-path.txt`).
- Real generated-map editor: supported roof extension, exact Undo/Redo and terrain/actor/resource save/reload. Infantry and tank subsequently traversed the saved extension in both directions.
- Normal and shaded client views show the two raised home areas and valley. New generation is required; saved maps are not rewritten.
- Continents, Two Sides, Water Basins and the recently updated Archipelago 4P42 exports remain byte-identical in both `map.bin` and `map.yaml`.

Local Release installed after confirming no OpenRA process was running: 0 errors, 3 existing warnings. Modular tests: 180/180; `git diff --check` passed. Only the newly created diagnostic fixtures were moved out of the playable map list.

This supersedes the blocked-ridge Hill Fortress design in `shared-mountain-contours.md`; Battle Nexus remains unchanged. No blanket all-seed, multiplayer, fairness or performance certification is implied. The separately reported Continents/Archipelago coastal fragments and Mountain Valleys expansion walls are not fixed by this change.
