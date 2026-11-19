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

- Isometric source commit: `c24b85eb`; release preparation: `cb343bac`;
  release merge/tag `v0.97.4`: `bfe6a7793c556fe2d0faf3ecfbc3e27012bca15b`.
  Isometric and tournament-bot branches plus tag were pushed atomically.
- Packaging run https://github.com/patrickwieth/YMCA/actions/runs/44423406905
  succeeded for Windows installer/portable, Linux AppImage and macOS DMG, followed
  by stable manifest publication and the regular game-server deployment.
- All three branch/tag CI runs succeeded (Linux Mono, Linux .NET, Windows .NET).
- Stable publication commit: `00cbc86b`. The raw public URL initially returned an
  older CDN-cached response; API and a fresh public request subsequently confirmed
  v0.97.4 (2026-11-19 00:01 UTC).
- Independent TCP handshakes on 45.136.28.158:1234 and :1235 advertised v0.97.4.
- The actual published Windows portable ZIP was downloaded and inspected: both
  versioned and generated map folders, the automatic-download server trait, exact
  approved radar PNG bytes and ATWR foundation binding were present.
- Release: https://github.com/patrickwieth/YMCA/releases/tag/v0.97.4 . Public notes
  are concise English, grouped under Features and Bug Fixes.

**Tournament bot rollout completed on November 19.** The user explicitly cancelled
T009 and requested readiness confirmation before creating a replacement. After
checking no game was playing and no other match was active, T009 and M0020 were
marked Cancelled with before/after backups; player registrations, completed
results, bracket history and map archives were preserved. Backup directory:
`data/recovery/retire-T009-20261119T060513Z-9bba7301/`.

The normal guarded deployment succeeded without FORCE_DEPLOY or bypassing its
match guard: https://github.com/patrickwieth/YMCA/actions/runs/44469584179 .
Discord login/command registration and live tournament host v0.97.4 were verified.

Post-deployment validation succeeded:
https://github.com/patrickwieth/YMCA/actions/runs/44470864796 . The actual deployed
Linux engine generated both 2- and 4-player maps and started isolated dedicated
hosts with their exact UIDs. The public HTTPS archive returned matching length,
SHA-256 and ETag. Tournament state was unchanged by these diagnostics. Preliminary
probe attempts omitted production ENGINE_DIR and reserved-player startup settings;
the harness was corrected rather than changing the working production settings.

Fresh native GUI profiles downloaded from the updated live HTTPS endpoint, passed
player/late-spectator/explicit-opt-in cases and started an actual local test match
without manual map installation. Evidence: temp `ymca-map-download-probes/live0974-final`.
No replacement tournament was created by the assistant. All participants must use
v0.97.4 for the updated tournament host. The earlier recovery is documented in
`tournament-M0020-recovery.md`.
