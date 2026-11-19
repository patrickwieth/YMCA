# Portable replay maps (local, unreleased)

## Behavior

New client and dedicated-server recordings capture the complete map package when
play starts and include it in the replay metadata footer when the recording is
finalized. This includes map.yaml, map.bin, map.png and other package entries, not
just the seed. It does not depend on a later generator producing the same output.

When watching a replay with a missing map:
1. Restore its embedded archive, checking the exact recorded map UID.
2. For older recordings without an archive, read the last pre-start lobby settings,
   decode the existing bounded generator recipe, regenerate and verify that UID.
3. If neither succeeds, do not substitute a similar map. The normal replay
   compatibility dialog reports an unavailable map; diagnostic details are logged.

The replay browser no longer disables Watch solely because the map is absent.
Browser and direct-launch paths invoke restoration. Existing installed maps are
reused. Restored packages use uniquely owned session files and existing
fingerprint-checked exit cleanup; originals and the input replay are not edited.

**Matching game/mod versions are still required.** A bundled map cannot provide
old unit rules or simulation code. Existing version checks are retained. Legacy
replays with no embedded archive AND no saved generator recipe cannot be recovered
from a title/seed alone. Large/unsupported packages that cannot be embedded are
logged; ordinary generated maps are well within the limits below.

## Implementation and safety

- `tools/patches/portable-replay-maps.patch` extends engine ReplayMetadata with a
  tagged optional binary archive after the existing game-info string. The original
  metadata version and order stream remain unchanged. Older readers can still
  read the game-info string and skip the extension using the footer length.
- Archive bytes are read lazily, not while enumerating the replay browser.
- A mod-provided `IReplayMapProvider` implements capture/restoration. Mods without
  a provider retain ordinary recording behavior.
- `ReplayMapProvider` preserves package entry order (part of the engine map UID),
  validates compressed/expanded sizes, entry names, duplicates, actual lengths
  and the UID before loading the restored map. No archive entry is extracted into
  an arbitrary filesystem path.
- Limits: 16 MiB compressed, 64 MiB expanded, 256 entries; bounded legacy lobby
  scanning (10,000 packets, 64 MiB scanned, 1 MiB per packet). These are safety
  limits, not a new transport for unrestricted uploaded data.
- Maps still have the usual trust requirements of custom OpenRA maps. UID checks
  establish identity, not authenticity of a replay received from another person.

**Build/install requirement:** apply all patches via
`sh tools/prepare-engine-patches.sh` and rebuild the engine and mods together.
The CA DLL now references a new engine interface. Do not install only the CA/Cameo
DLLs into an unpatched engine. The older two-DLL staged LAN installer is not an
installer for this feature. No running user game or installed runtime was replaced.

## Validation, 2026-11-18

- Original `ca-2026-11-18T160119Z.orarep` preserved unchanged, SHA-256
  `81cc9106d5419d58d4a1e38f13f64fdab660fd0939a362ed0c71cd07ef24c6b5`.
- On two separate fresh profiles, both legacy-recipe restoration and embedded-map
  restoration played through tick 55835 (after the win/loss at 55804), without observed crash
  or playback desync. No map was manually installed in either profile.
- Exact restored UID: `84aab46fdff024bc90f21835e1a3fd51797bd20a`.
  Its complete embedded map archive adds 16,940 bytes.
- Actual new LAN match recording finalized by normal disconnect, not force-kill.
  Recorded map UID `310d426f3b2249dcf030714fbf3a4819ba3d284c`, archive 9,283 bytes.
  Only the resulting .orarep was copied to a fresh profile. The real Extras ->
  Replays -> Watch UI restored it and playback advanced through tick 100.
- Native utility `--check-replay-maps REPLAY OUTPUT` verifies legacy recovery,
  metadata/archive round trip, UID mismatch, corrupt archives, unsafe paths,
  duplicate entries, excessive expansion, read-only files and malformed footer/archive lengths.
  Use an isolated engine/support directory; OUTPUT must not already exist.
- Modular tests: 341 passed. LAN creation, generation, guest/late joining, map
  change and match-start checks also passed with recording enabled.

Evidence is under `docs/validation/portable-replay-maps/`; full isolated playback
logs and instrumentation remain in workspace `.tmp/replay-portable-test/`.
Two preliminary browser probe attempts had fixture setup errors (missing probe
assembly and an omitted callback); the successful probe uses the real menu
callbacks instead. These were not production replay failures.

## Related screenshot fix

`Warpable.ISelectionBar.GetValue()` could return NaN from 0/0 when low HP rounded
its scaled erase threshold to zero. The isometric bar renderer converted that
invalid endpoint into the long cyan line. The original replay exposed NaNs on
Soviet infantry with bar color `40FFFF`. The getter now returns a bounded, finite
UI value; damage, thresholds, timers and synchronized state are unchanged. Both
full playback runs scanned selection bars without observing invalid values.
The normal diagonal isometric health bars are intentional and remain unchanged.

All changes are local. No release, push or deployment has been performed.
