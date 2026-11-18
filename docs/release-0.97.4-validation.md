# v0.97.4 validation and rollout

## Authorized scope

User authorized committing, pushing and releasing the tested work, explicitly
including Isometric changes. Release tag must be on tournament-bot. Active matches
must not be interrupted and match-safety guards must not be bypassed.

Included: tournament HTTPS map download for players/spectators, replay map
embedding/recovery, peer-hosted LAN recipe synchronization, generated-map folder
packaging correction, stale remote-map response protection, opt-in AppImage link
registration, duplicate tournament-join feedback/atomic modal registration,
UI-only Warpable bar correction, approved GDI Radar recolor and ATWR foundation.
Existing v0.97.3 Isometric features remain included.

Excluded: unfinished art studies and the unrelated locally modified
`mods/ca/bits/nod/buildings/conyard.png`. Its working-copy checksum is preserved.
No gameplay balance change is intended. Championship news still correctly refers
to 20–22 November 2026 (checked before release preparation on November 18).

## Checks completed before publication

- CA/Cameo builds against the isolated patched pinned engine.
- 365 Modular tests; 29 ordinary bot tests.
- Two explicit real host tests: generated 1v1/2v2 maps, exact UID, download
  receipt, fixed map pool, HTTP map delivery and SHA-256.
- Real Windows GUI download test: cold-cache host and guest, fresh late spectator,
  downloads disabled/explicit consent, exact archive SHA/UID, hot-loaded map and
  actual match simulation on both players. Evidence: temp
  `ymca-map-download-probes/pass08` and `docs/validation/tournament-map-download/`.
- Native Skirmish wizard, custom settings, ordinary-map chooser, cancellation,
  repeated map changes and opponent retention passed.
- Dedicated waiting-lobby/generator/admin/guest/late-join regression passed;
  native foundation binding checks passed.
- Native replay tests restored both legacy and embedded maps without preinstalled
  maps, passed the recorded win/loss, and launched a newly recorded portable file
  through the real replay browser. See `portable-replay-maps.md`.
- Replay/archive utility checks include malformed lengths, unsafe/duplicate entries,
  excessive expansion, SHA/UID identity, read-only files and installed-map priority
  over late repository responses.
- Eight AppImage registration tests passed on an isolated Ubuntu runner, including
  real GIO launch and real xdg-mime in temporary HOME/XDG directories:
  https://github.com/patrickwieth/YMCA/actions/runs/44419119603
  Local Windows runs pass the shell tests and skip the two Linux-only cases.
- Complete six-patch stack applied to pristine files from engine pin
  2dfb43c864804ede5efca9e31f7add52f7233aa3; all repeat-application checks passed.
- Version-stamping regression preserves the stable generated folder across version
  updates. Radar/ATWR tests passed; original native art evidence is preserved.

Native download testing exposed and corrected the stale repository-response race
before publication. Preliminary test-harness failures (missing mod-search env,
URI forwarding between probe processes, unsafe probe cache enumeration) were
fixed in the harness; probes now use independent Launch.Connect sessions. Linux
native testing also caught literal-percent executable lookup in Desktop Exec;
the registered launcher now safely handles special filenames and URI arguments.

Historical raw-source supplied-art audit failures recorded for v0.97.3 are not
claimed fixed by this release; the current targeted art/binding checks above were
run rather than claiming that entire historical audit is green.

## Rollout status

Source preparation complete; commit/tag, package publication, stable manifest,
public game-server deployment and separate tournament-bot rollout must each be
verified and recorded below. Do not infer deployment from release creation.

At 23:36 UTC, T009/M0020 was still WaitingForPlayers. The user has been asked for
permission to renew that waiting lobby during the version update, only if no game
has started; the bot deployment guard must remain respected. The earlier M0020
live repair and backup are documented in `tournament-M0020-recovery.md`.
