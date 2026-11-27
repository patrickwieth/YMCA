# Map review — local follow-up, 2026-11-25

Implemented and installed locally in **Game**. No release, push or tournament deployment. Generate new maps to see the changes: existing packages, archived UIDs, replays and handcrafted maps remain unchanged.

## Agreed scope

- **Continents is accepted**, explicitly reconfirmed by the user; no resource changes are pending. Its intended progression is an early rush, then a transition to oil derricks and islands if that rush fails. It remains the deliberately resource-starved generated archetype.
- **Hill Fortress is accepted. Migration is also accepted for now**, per the user's latest review; no further Migration changes are pending. Archipelago is accepted and explicitly removed from the backlog; long-term playtesting is not a tracked pending task.
- **Mountain Valleys and Water Basins keep their layouts and economy.** Mountain Valleys additionally received a native plateau-access regression, not terrain changes.
- **Battle Nexus layout is accepted.** Only upper-surface usability is changed; do not redesign its ring, walls, gates, starts or economy.
- Scrin building differentiation/artwork is closed for now at the user's request. **Building remap/player colors remain open** as a separate topic; the limited accent-color approach was already tried.
- **Urban Warfare** is planned as a dedicated mixed-density city/village/outer-settlement map with roads and parked civilian vehicles; see `urban-warfare-map.md`. The collection is now supplied in `mods/ca/bits/misc/buildings/` and initially categorized in `urban-art-inventory.md`; extraction/integration remain pending. The assets need cutouts and integration, followed by neutral-building garrisons, player-color flags and infantry fire. See `urban-garrisons.md`.

## Mountain Valleys 1on1 size/ramp follow-up

Mountain Valleys automatic 2P maps now use **112×112 instead of 80×80**. Home ramps
prefer lateral exits and may not face the opposing spawn; invalid fits retry the
layout rather than falling back to a forward ramp. Desert stays 80×80 as the
intentionally compact rush map. See `mountain-duel-followup.md` for multi-seed,
control-map and native movement checks. Existing map packages are unchanged.

## Later visual follow-up

The existing Plains preset is now displayed as **Desert**, with sandy ground and
sparse Dune-style trees. Its accepted layout/economy below are preserved. Mountain
Valleys gains Barren ground/roofs/ramps and sparse dead trees; generated Two Sides
gains sparse groups matching its handcrafted variant. The Handcrafted Maps picker
now contains only the three curated maps. See `map-biome-followup.md` for native
checks. Glitters' newly reported angular edges remain open pending an identified
problem location; no speculative shape change was installed.

## Plains (now Desert): separate additional patches

In Patches mode each player now has one starting field and **two owned expansion fields**, plus `max(2, players / 2)` contested fields for the map. Thus 1v1 has **eight separate fields instead of four**; 4P has fourteen. The new owned field and each contested field have 49 cells. Existing starting-budget policy remains 81/113/149 cells; first expansion remains 49/81/113 cells.

Owned fields retain equal group budgets and comparable native approach lengths. A two-cell native resource gap prevents expansions joining older fields. Contested cells require two reachable contenders, an approach of at least 20 cardinal steps, and no more than eight steps of difference between the nearest two approaches. Both contenders receive postprocessing access/resource-loss guards. These fields are separate opportunities, not enlarged starting blobs. Infinite/finite and None/Glitter controls remain intact. Changing economy can change the accepted generation attempt for a seed.

### Plains oil follow-up — locally installed

The user approved Plains terrain/resources and requested outer oil clusters instead of individual home-side derricks. `PlainsOilClusters` now chooses the two least-occupied opposing corners for 1v1. With more players it projects start positions to the perimeter and targets the outer midpoint of each gap between neighboring starts. Each cluster contains **3 derricks for Sparse, 4 for Dense**; None remains empty. There are two clusters at 2P and one per player gap for larger games.

The 2×2 native footprints and one-cell access margins must fit clear ground, without occupying resource fields, protected approaches, plateaus or strategic reservations. Clusters stay outside the safe starting core and all starts retain native ground access. No movement blockers or resources are cleared to force a patch-mode cluster into place. Glitter mode only clears the actual building footprints, as before. Cluster metadata survives cloning and is validated again after map postprocessing; the old unique-owner oil constraint remains in place for Mountain Valleys, not for contested Plains oil.

Validation: `--test-plains-oil-clusters OUTPUT.json` covers 72 complete layouts (2–12/16 players, Sparse/Dense, seeds 1/42/43), plus the existing 36-case starting-economy/option regression. Native 2P/4P Sparse and installed 4P Dense runs verify oil access, actual movement, harvester routes and construction-yard footprints. For 2P/4P seed42, old/new exported tile, height and resource arrays are byte-identical. Evidence: `art-source/map-balance/plains-oil-clusters-validation/`. Only Plains oil placement changed; existing saved map packages are not rewritten.

## Two Sides: distance, river crossing and supply

- Auto-size adds 32 to each dimension: **112×112 at 2P**, **128×128 at 4P**.
- Starts sit deeper behind the banks. Representative opposing starts are now approximately **56 cells apart at 2P** and **64 at 4P**, versus 38 and 42 previously.
- User follow-up: the first pass still clustered the fields and had too short a ford. The final ford uses a constant-width strip in **native world-cell axes**, running three cells along the river for each cell across it, instead of an angle in the anisotropic map-memory grid. Its center remains offset along the river. An eight-neighbor minimum wet-crossing regression requires more than 1.4 times the river width; measured 2P crossings are 21–24 wet steps and 4P crossings 27–29 in the regression seeds. This tests the ford itself, not just a longer approach.
- Every patch now uses the full original radius-7 shape (**135 resource cells each**). The starting patch stays near each spawn; the two additional patches per player are distributed across that team's bank by maximizing separation from existing fields, with a minimum distance from starts. Infinite patches each have a regenerator. Existing launch/cliff-shore cells are excluded from resource placement and their shore flags are no longer erased by patch painting. Cliff artwork and shore-classification rules are retained.
- For seeds 1/42/43, 2P has six fields/**810** resource cells and 116–120 native cardinal ground steps between the representative opposing starts. 4P has twelve fields/**1,620** cells and 134–138 steps. These are grid-path diagnostics, not unit travel time or diagonal shortest-path lengths. The initial smaller-patch results remain archived in the earlier follow-up evidence; the current revision is in `art-source/map-balance/twosides-distributed-validation/`.

## Battle Nexus: real usable roofs

The authoritative accepted Nexus mountains are stored in `plan.PlateauSurfaces` by `RubberduckMountainTopology`, including reserved low cliff feet. They do not normally use the legacy mountain fallback. Export now changes only playable height-4, flat roof surfaces from blocked Cliff to Clear. Low cliff feet, height discontinuities, closed face geometry and non-playable backing remain protected. No ramps are added.

Native tests on 2P and 4P Nexus:

1. Carryall physically picks up and delivers an MCV to a plateau.
2. MCV uses its actual deployment activity and creates a construction yard there.
3. An adjacent power-plant footprint passes ordinary terrain and base-proximity checks; the fixture then places the power plant there for render review (it does not test the production queue).
4. Carryall picks up and delivers a tank; the tank actually moves along the upper surface.

The same pickup/deployment/movement test passes on unchanged Mountain Valleys, including 4P.

## SSM: retain the terrain weakness

No SSM weapon statistics, flight settings or submunitions were buffed. `HonestJohn.multi` remains MissileCA with its existing flight characteristics; the misleading `LaunchAngle` YAML entry was not used as authorization to change its flight. The existing terrain fire controller already includes it in conservative corridor checking and stance-aware local position search.

The SSM-specific native regression verifies actual damage after permitted repositioning, stationary defensive/return-fire/hold-fire behavior, stance changes, near/far explicit orders and a trapped case. Its long range required isolating the return-fire test farther away: otherwise a different SSM could legitimately hit that target after finishing its own fight.

Additional 2P and 4P **actual exported Nexus** tests locate a blocked firing position with a locally reachable alternative, then run a normal explicit SSM attack and require both movement and target HP loss. The test does not substitute a fake weapon or disable terrain collision. This confirms useful repositioning without granting rock penetration or terrain-following guidance. It is not a guarantee that every hill has a reachable local solution.

## Evidence and reproduction

`art-source/map-balance/followup-validation/` stores the economy matrix, native result JSONs, screenshots and unaffected-map comparisons.

- `--test-map-economy-followup OUTPUT.json`: 30 complete layouts, 2/4/8/12/16 players, seeds 1/42/43; field counts, separation, owned/contested guards, Two Sides distance and detour checks.
- `--start-balance-test`: original complete economy/option/clone/access regressions.
- `--two-sides-test`: full team-bank, checkpoint, deterministic-generation and coastline regressions.
- `--symmetric-oil-test`: full oil placement regression.
- `tools/prepare_start_balance_probe.py` plus `tools/test-handcrafted-map-runtime.ps1 -WaitForMovement`: actual 2P/4P harvesters, routes, oil access and existing yard-footprint checks on installed Plains/Two Sides.
- `tools/prepare_plateau_access_probe.py INPUT.oramap OUTPUT.oramap RESULT.txt` plus `tools/test-refinery-docking.ps1`, with `YMCA_PLATEAU_ACCESS_PROBE=1`: isolated carryall/building/movement probe; Nexus also tests SSM combat. Never install diagnostic fixtures as ordinary maps.
- `tools/test-cliff-fire.ps1 ... -Mode ssm`: isolated SSM stance/obstruction regression.

Before installing, exported the same 4P seed42 for Continents, Hill Fortress, Migration, Archipelago, Mountain Valleys and Water Basins with the old installed binary and the new isolated binary. **map.yaml, map.bin and map.png are byte-identical for every control.** Nexus 2P seed42 comparison changes only 577 former Cliff roof tiles to Clear; height/resource arrays remain identical.
