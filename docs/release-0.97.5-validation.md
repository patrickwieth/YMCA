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

## Publication status

Release pipeline and live deployment verification are pending. Tag must originate
on tournament-bot; all three platform builds precede stable manifest publication
and game-server deployment. Tournament-host deployment is separate and guarded.
Do not report servers/packages ready until actual workflow/live checks succeed.
