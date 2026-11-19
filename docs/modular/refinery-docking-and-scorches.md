# Refinery access and scorch palettes — November 19, 2026

Implemented in Game / isometric, merged to master and published in v0.97.5.
See `../release-0.97.5-validation.md` for package and deployment verification.

## Refinery docking

`rules/refinery-docking.yaml` is loaded after the final ground-mask layer, not
against the obsolete base-building footprints. `DockHost.DockOffset` is a world
vector from the actor center; it is not a cell offset. The resolved primary
bays are now cell (1,2) for Allies, Soviet, GDI, Nod and Scrin, and (2,1) for China.
Nod's 158-unit center adjustment is explicitly compensated. GDI/Nod no longer
use the legacy drag animation that displaced the harvester off its service bay.

The invisible auxiliary actors previously had a default blocking footprint.
They now have a passable footprint and dock at their own cell center. All bays
are distinct and reserved against other building placement. Existing numbers of
docks and their queue capacities are retained: two per refinery, four for China.
China has only three suitable cells in its existing mask; its fourth bay now
reserves (3,1). Dimensions become 4x3 with center compensation (-362,-362), retaining
the original artwork position and all original occupied body cells. Its free
harvester spawn moves from the blocked (1,1) cell to (2,1).

Obsolete Scrin docking fields on `Refinery` (which no longer owns those fields)
were removed. Its actual docking configuration is on `DockHost`. Prices, storage,
resource yields, load/unload rates, supplied artwork and other factions' units
are unchanged. Arbitrarily walling in every entrance can still block a refinery;
this fixes the building's own invalid destinations, not player-built enclosures.

Validation:
- `--check-refinery-docking`: all six final actor definitions, centered primary
  destinations, nonblocking helpers, distinct reserved bays, walkable exits and
  free harvester spawn cells pass.
- Real local simulation: all six refinery types, three loaded harvesters per dock,
  **42 actual unloads and 42 departures**, including auxiliary dock contention.
  Native helper lifetime and normal harvester undocking are used, not mocked paths.
  The final run also enables China's local minefield upgrade to verify friendly
  mines on service cells do not block deliveries/departures.
- 365 Modular tests pass; matching engine/mod build succeeds.
- The broader pre-existing `--check-bib-occupancy` check stops on unrelated
  `ftur: unreserved bib cell`. It is not represented as passing. Its refinery
  invariant now calls the new reachability check instead of demanding old offsets.

The native harness initially needed fixes: ignore the harvester itself in a spawn
occupancy query, and observe completion externally because `Harvester.OnDockCompleted`
replaces queued activities with its normal harvesting loop. Departure means actually
reaching the outside parking cell, not requiring an idle economic actor afterward.

## Scorch / crater / flame palettes

The earlier legacy-RA ground-palette fix is retained. All six scorch sprites and
all five depths of each of the six crater sprites were resolved/exported for each
of the nine retained tileset palette definitions. All combined smoke/flame sequences
were resolved too. Exported ground colors and all fire/smoke animation frames were
visually reviewed. No indiscriminate recoloring of ground scorches was needed.

A separate real defect was found: `SCORCHBLACK.SmokePalette: scrineffect` referenced
no palette definition. It now uses `scorch-black`, derived reproducibly from the RA
palette by `tools/art/generate_scorch_black_palette.py`. Chromatic flame ramps become
shaded charcoal; neutral smoke colors and transparency remain intact. All 91 smoke
frames compare identically between the regular and black palettes. Original palettes
and sprite files are untouched.

Native rendering was checked on **both currently enabled tilesets**:
RUBBERDUCK-TEMPERATE and RUBBERDUCK-TEMPERATE-SHADED. The other seven palette definitions
are legacy/inactive in the manifest: their frames/palettes were loaded and reviewed
as exported images, not claimed as playable-map native tests. An attempted Snow
fixture fell back to a normal map and was rejected by the harness timeout; it is
not counted as validation. No production tilesets were enabled for testing.

Evidence: `art-source/refinery-smudge-review/` contains the traffic log, complete
frame review sheets and native crops from the two active tilesets.

## Reproduce

Build against the matching patched engine, then from Game with ENGINE_DIR and
MOD_SEARCH_PATHS configured run:

```
OpenRA.Utility ca --check-refinery-docking
OpenRA.Utility ca --check-scorch-palettes OUTPUT_DIRECTORY
python tools/make-refinery-smudge-probes.py ABSOLUTE_FIXTURE_ROOT
```

The fixture generator creates `refinery.oramap` and two `scorch-*.oramap` files.
Run each with `tools/test-refinery-docking.ps1 -EngineDirectory ENGINE
-MapPackage FIXTURE_ROOT/CASE.oramap -OutputDirectory FIXTURE_ROOT/CASE`.
Use fresh output directories. Only processes created by the harness are stopped;
content is read through a junction, with separate settings/maps/logs/screenshots.
Probes are opt-in map traits, absent from normal gameplay rules. They additionally
require the process-local `YMCA_REFINERY_SMUDGE_PROBE=1` gate, set/restored by the
harness, so a downloaded map cannot activate their filesystem diagnostics.
