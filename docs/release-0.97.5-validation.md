# v0.97.5 release validation

User authorized committing, pushing, merging to master and releasing after approving
the corrected rock silhouettes on 19 November 2026.

## Scope and preservation

- Approved rock/unit/resource occlusion, refinery docking/scorch corrections, GDI
  foundation joint colors and automatic Windows engine patch preparation included.
- Previously deployed tournament bracket/attendance/post-game work retained.
- Existing Battlefield News URLs and bundled news retained in the single WebServices
  block, alongside UpdateManifest. Championship dates remain 20–22 November 2026.
- Protected `Game/mods/ca/bits/nod/buildings/conyard.png` is not committed or changed.
  Unfinished art studies, Blender backups, caches and diagnostic maps excluded.
- Master's old local mirrors were preserved before integration in
  `.tmp/master-before-release0975.zip` and the named pre-v0.97.5 Git stash. The personal
  replay was returned to its original location. Merge conflicts used the current,
  already released download/replay/tournament implementations rather than obsolete
  master copies; the obsolete manual-map-installation README duplication was removed.

## Validation before publication

- Normal Release engine+mod build in Game-release: passed, zero errors.
- Modular tests: 365 passed. Tournament bot tests: 95 passed.
- Native corrected slope/full/partial-occlusion pixel comparisons passed, alongside
  native movement, cloak/fog, resource-depletion and two-zoom checks. See
  `docs/modular/rock-occlusion.md` and its retained evidence.
- Refineries: resolved docking checks and 42 native deliveries/departures already
  recorded in `docs/modular/refinery-docking-and-scorches.md`.
- GDI masks and native connection lifecycle evidence retained under
  `docs/modular/gdi-joint-remap.md`.
- Existing unrelated broad bib occupancy failure (`ftur: unreserved bib cell`) is
  not claimed fixed. Existing NuGet advisory and compiler warnings remain.

## Live preflight and deployment safety

Read-only host checks 44599505830 and 44599836012 confirmed no active/retained
tournament matches, unchanged v0.97.4 tournament container uptime, and a public
restart scope limited to screen sessions YMCA1/YMCA2 (ports 1234/1235). Cameo has
an active match on 1236 and must remain untouched. Public YMCA listings were empty.
The remote public script itself had no player guard, so both release/manual SSH
entry points now fail closed on occupied ports or a failed `ss` query. Local mocked
empty/occupied/query-error checks exercise both entry points.

An initial maintenance workflow content upload failed; the subsequently dispatched
run 44599395540 used the old deployment definition. It was cancelled during Docker
COPY, before rollout. Both following read-only checks confirmed the existing bot
container remained up for eight hours, unchanged. No match/state changes occurred.
The corrected maintenance workflow is read-only.

## Published and verified

- Tag `v0.97.5`: `bc8604c4`, on tournament-bot with master/isometric ancestry.
- Packaging run **44600367576**: Linux, macOS, Windows, stable manifest publication
  and public game-server deployment all succeeded. The server restart guard passed.
- Windows installer/portable ZIP, AppImage and DMG are available. All three stable
  manifest checksums match the GitHub asset SHA256 digests.
- Downloaded Windows portable package was inspected for v0.97.5, refinery rules,
  attendance server and matching engine silhouette API. Its native full/partial/ramp
  pixel comparisons and resolved refinery utility checks passed. Portable ZIP SHA256:
  `827320e2ccac405908b1dea181ca63ef4d08daff8ab6e377d8f1726874aae308`.
- Live public ports 1234 and 1235 both advertise v0.97.5 with zero connected players
  or spectators at the final check. Only YMCA1/YMCA2 were restarted; Cameo is outside
  the script's restart scope.
- First separate bot rollout **44604225578** revealed the Docker/Compose host version
  was still pinned to v0.97.4. No active matches were present. Follow-up commit
  `9de05030` aligns the host with v0.97.5 and adds a client-release/host-packaging
  version consistency regression (96 bot tests now pass). Published game packages
  and tag remain unchanged.
- Corrected guarded bot rollout **44605113405** succeeded, including Discord command
  registration and explicit v0.97.5 host verification. Read-only check **44605792236**
  at 20:09 UTC confirmed v0.97.5, the attendance trait and no active/retained matches.
- Public tournament index and T010 bracket return HTTP 200 with CSP. No attendance
  timer was activated, tournament created, match result changed, or safety guard
  bypassed during this release.

Release: https://github.com/patrickwieth/YMCA/releases/tag/v0.97.5
