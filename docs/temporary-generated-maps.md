# Session-lifetime wizard maps

Maps created by the Skirmish generator are now temporary for the current application session. They use unique `ymca-session-<guid>.oramap` filenames, so generating a seed never overwrites a previously saved permanent map. The displayed title and deterministic map contents remain unchanged. Utility/export commands retain their existing permanent filenames unless explicitly asked for temporary output.

The wizard registers each successful export before loading it into the map cache. `Game.OnQuit` removes only registered, unchanged files after the world and mod/map-cache packages have been disposed. Maps remain available throughout matches, lobby switches and returns to the menu.

Safety:
- No folder scan or cleanup based only on Author/title/seed.
- Existing generated maps, downloaded maps, other sessions' files and saved copies are not registered and remain untouched.
- SHA-256 fingerprints preserve registered files that have changed since generation. Save As under another filename is the explicit way to keep a map.
- Missing files are tolerated; locks/permission failures are logged and do not prevent shutdown.
- A crash or forced termination can leave temporary files behind. There is no startup scavenger or claim of crash-proof cleanup.
- Deleting the map may make a replay unavailable until the matching map is restored/regenerated. Replays themselves are not deleted.

Validation: `--temporary-generated-map-test` covers ownership, ordinary-map rejection, edited/re-registered files, saved copies, concurrent-session isolation, absent files, repeated cleanup and Windows lock/retry behavior. An isolated real client loaded a uniquely named fixture and exited via `Game.Exit`; its package was removed by the actual `OnQuit` callback after disposal, without killing the process. Evidence: `%TEMP%/ymca-continents-feedback/{temporary-map-tests.log,quit-test.log}`.
