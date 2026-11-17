# Legacy map and shellmap safety (0.97.1)

Reported failure: an older Island Wars II - Wasteland shellmap loaded and crashed
with `This frozen actor has no footprint` for `rushouse2`. The user's exact map
file and exception log have not been collected; its installation path is unknown.
The fix prevents this obsolete shellmap from being selected, rather than changing
rushouse2 without a reproduction.

- Only RUBBERDUCK-TEMPERATE and RUBBERDUCK-TEMPERATE-SHADED remain registered.
- Legacy bundled maps and retired menu scenes are preserved outside the runtime
  mod directory in `art-source/legacy-map-archive/`, with SHA-256 inventory.
- `ShellmapName: liberty-stream-shellmap` restricts menu selection to that bundled
  system-map package. Old compatible system maps and user maps with the same
  package name cannot win the selection. No silent fallback to a different map.
- MapPreview marks maps with unregistered tilesets unavailable, including files
  left in old installations or user map directories. User files are not deleted.
- `tools/patches/legacy-map-safety.patch` implements the engine checks and manifest
  field. `tools/prepare-engine-patches.sh` applies it idempotently after the base
  patch; builds must prepare both before loading the new manifest.
- `--check-active-maps` injects stale system/user shellmaps and verifies exclusion.
  The actual native Liberty menu was also run in an isolated 1000-tick probe.
- Kirov stream cadence is reduced to 90% of the old rate, carrying fractional
  ticks: 83,83,84 ticks instead of 75. The initial formation and unit speed remain
  unchanged; statue destruction still stops reinforcements.

Not part of this release: unfinished tournament generation and the dedicated
multiplayer map-generation/transfer flow. Neither is claimed fixed here.
Historical shellmap tools/docs may still refer to the archived development maps.
