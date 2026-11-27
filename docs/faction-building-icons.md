# Building cameos across factions

Local Game worktree only; no release, push or deployment.

## Scope

The approved Allied treatment now covers GDI, Nod, Soviet, China and Scrin buildings:
192×144 PNG textures in the existing 96×72 logical production cells, the same blue
background/shadow treatment and classic OsShp captions. The approved Allied PNGs
and their bindings remain unchanged. Shared income buildings reuse the approved
Allied portraits across the remaining factions.

The former Soviet exceptions are complete: Nuclear Powerplant and Barracks now
use supplied artwork and matching cameos (99 portraits, 81 direct PNG sources);
the Allied Tech Center was also replaced (see `supplied-barracks-tech-center.md`). Allied Helipad/Radar were subsequently replaced from newly supplied graphics;
see `allied-radar-helipad.md` for those authorized exceptions. Research/plug/mines
icons (except the subsequently requested China Radar Upgrade), vehicles (including China's MCV) and disabled experimental GLA are outside
this building-art pass. Existing disabled legacy structures remain disabled.

## Sources and reproduction

- `art-source/faction-building-icons/catalog.json`: reviewed labels/actor groups.
- `source-bindings.json`: engine-resolved source filenames and source method.
- `provenance.json`: actual crop rectangles, source hashes and actor coverage.
- `tools/prepare_faction_building_icons.py`: reproduces portraits, contact sheets
  and the icon-only sequence overrides. Does not regenerate building artwork.
- Runtime: `mods/ca/bits/faction-icons/` and
  `mods/ca/sequences/faction-building-icons.yaml`.

81 portraits use authoritative PNG files directly (including the subsequently requested bounded China radar cut), including GDI's authoritative
building folder. Twelve multipart defenses use archived native compositions;
six legacy SHP portraits use archived native frames. These limited-resolution
legacy sources are not presented as newly created high-resolution artwork.
The initial all-building screenshot approach was superseded by direct PNG inputs:
old `*-review.png` sheets are exploratory, `*-direct-review.png` is current.

Native composites are archived under `sources/*-composed.png`; they omit bibs,
use body facing zero/turret facing 768 and correct the preview-only extra scaling
of world-space turret offsets. Their flat capture background is keyed out before
the same cameo treatment. No live building render/placement code was changed.
The optional `FactionCameoExportProbe` and source widget run only in a diagnostic
fixture with `YMCA_CAMEO_EXPORT=1`, not normal World rules.

No supplied building PNG was rewritten, including the forbidden Nod construction
yard, approved GDI AA pixels, or the user's oil-platform waterline. The original
sources remain in their authoritative folders; generated images are icon-only.

## Validation

`test_faction_building_icons.py` verifies approved Allied pixel hashes, source
hashes, dimensions, resolved bindings, Soviet exceptions, gameplay hashes, and
complete non-icon sequence equality. China's advanced reactor uses `iconadv`,
not `icon`; that portrait is explicitly excluded from the non-icon comparison.
Snapshots are stored alongside the catalog; refresh the audit deliberately when
changing art, rather than silently updating preservation baselines.

Native Building/Defense galleries passed for eagle, blackh, russia, chinatnk and
reaper at actual UI scale 1.25. Completed screenshots and results are in
`validation/`. These fixtures add `ICONREVIEW.*` actor aliases to expose every
variant without modifying production prerequisites or merging over legacy raw
inheritance keys. Original actors can also appear, so duplicate entries in the
review screenshots are fixture-only, not additions to normal production menus.
China's ordinary MCV is correctly left as a vehicle icon.

The reusable `AlliedBuildingIconReviewProbe` now accepts expected actor lists and
explicit legacy exceptions; its default Allied fixture behavior is preserved.
The icon pass left source originals, gameplay/footprints, disabled status and production queues
unchanged. The later Tesla collision/registration correction and four crop revisions
are documented separately in `soviet-building-followup.md`. Runtime data is available locally; no user process was stopped.
