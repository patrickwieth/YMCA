# Liberty Island menu battle

## Status

Implemented, live-tested and now selected in the main runtime. After the user
closed the existing game normally, the tested CA DLL was installed successfully
and the installed-runtime preflight passed. Liberty is Visibility Shellmap;
Two Sides is retained with Visibility0. Previous flags are backed up under
`art-source/liberty-shellmap/previous-selection/`. Normal game startup now selects
Liberty; `liberty-shellmap.cmd` can also rebuild, validate/select and launch it.

## Scene

- Separate Rubberduck map: supplied Statue of Liberty PNG is referenced directly,
  unchanged, at1.5x. It sits above/right of the menu center on an island.
- Six Allied flak guns, ten Aegis cruisers in the water in front of the island.
- Three Kirovs enter from the lower left every12 seconds, attack real targets and
  drop real bombs. Every third bomber prioritizes the statue; others target the
  island defenses and fleet. Targets are refreshed after destruction.
- Destroyed defenders return after10 seconds, statue after2 seconds. No defender
  is invulnerable. Bombers are capped at15 and retire after90 seconds if stranded.
- Map-local Kirov durability/speed and statue health keep the scene sustained.
  The ordinary game balance is unchanged.
- Ground impact marks clear every15 seconds to avoid permanently blackening the
  background island. The old Two Sides map does not enable this behavior.
- Straight shoreline banks account for staggered isometric rows; this avoids the
  half-cell beach spikes seen in the initial terrain draft.

## Actual checks

`--check-liberty-shellmap MAP` loads the real rules and complete sprite set,
checks ship water cells, occupied building footprints, counts and bomb ranges.
A6000-tick isolated live run passed; both sides scored kills and the statue stayed
present throughout the sampled run. A further2500-tick final run with visual
smudge cleanup passed with no Lua errors. Opening/later menu screenshots were
inspected. Evidence: `art-source/liberty-shellmap/review/`.

The live test exposed a pre-existing Kirov bomb configuration issue: an inherited
4-cell artillery minimum range exceeds the bomb's1-cell maximum. A map-local
`KirovBomb.MinRange: 0` fixes the cinematic attack. The normal-game weapon has NOT
been changed; a global fix remains a separate task.

The map's statue has idle/damaged/construction/dead/icon sequences; this includes
the construction path exercised by replacement spawns, not just initial loading.

## Tools

- `tools/prepare_liberty_shellmap.py`: native terrain export + map assembly;
  leaves the map disabled until validation/activation. Preserves supplied PNGs.
- `tools/run_two_sides_shellmap_probe.py --map liberty-shellmap --ticks 6000 --wait`:
  own settings/logs/runtime, self-exit; never stops unrelated games.
- `tools/activate_liberty_shellmap.py`: validates the installed runtime first,
  backs up prior visibility flags, then selects Liberty as the sole system
  shellmap. Existing maps are retained.
- `liberty-shellmap.cmd`: build, validate/select and launch after closing any
  currently running game normally.

No release, push or new commit. Production/release worktrees were not changed.

## Other user requests still pending

- Hide selection frames at a sufficiently zoomed-out level, retaining health bars.
- Multiplayer creation should use the singleplayer map setup wizard.
- Remove non-Rubberduck tilesets/maps from the active mod, retaining backups.
  Only preliminary unused copies for the multiplayer UI exist; these requests
  have NOT been implemented by completing this shellmap.
