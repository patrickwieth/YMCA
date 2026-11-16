# Supplied Scrin building PNGs — local isometric integration

Direct bindings from `mods/ca/bits/scrin/buildings/`:

| Actor | Artwork | Role |
|---|---|---|
| SFAC / SFAC.Nukular | headquarter.png | Construction Yard |
| REAC | power plant.png | Reactor |
| REA2 | advanced power.png | Advanced Reactor |
| PROC.SCRIN | refinery.png | Refinery |
| PORT | portal.png | Infantry production |
| WSPH | warp sphere.png | Vehicle production |
| NERV | nerve center.png | Radar / tier2 |
| SCRT | tech center.png | **Scrin Tech Center / tier3**; replaces earlier Foundry binding |
| GRAV | gravity stabilizer.png | Aircraft production |
| SIGN | signal transmitter.png | Signal Transmitter |

No Foundry actor or new tech/prerequisite is introduced: SCRT remains the existing
Tech Center with its existing name, costs, technology and behavior.
Repair/support structures and defenses not explicitly integrated remain unchanged.
REA2 now uses the separately supplied `advanced power.png`, not the regular Reactor
image. It is direct RGBA, scale1.25, Bottom offset0,-27, selection height204.
Its original source bytes and existing200 power/cost/health/prerequisites are preserved.
`advanced-power-placement.png` is the reviewed composed layout, not a live screenshot.
Field Manipulator, Rift Generator, Repair Bay and Storm Column are integrated in the
new-faction-building-art layer; see `../new-faction-buildings/README.md`. Foundry is
now deliberately unused. The new Tech Center is RGBA, Scale1, Bottom0,-56; its old
Foundry palette provider was removed. Seven indexed sources remain active.
SHAR now uses its independently installed25-degree pass22 model with fixed battery;
PTUR uses the new native ground turret from `art-source/scrin plasma`, pass05.

The original eight indexed PNGs are read only, never recolored or rewritten.
Headquarter received only the explicitly requested short, source-colored vein tips;
its original is preserved in `art-source/scrin-headquarter/headquarter-original.png`. `binding.json` records
source hashes, actor images, direct filenames, scaling, offsets and selection heights.
`rules-before.json` captures the prior resolved gameplay configuration.

## Embedded palette correction

The initial binding mistakenly treated the eight supplied indexed PNGs like RGBA
images. Their indices were interpreted by legacy Scrin player palettes, producing
wrong colors. Each indexed body now uses its own PaletteFromPng in World and
EditorWorld, selected specifically on WithSpriteBody. Existing death-animation
traits use the matching palette too. Icons, effect overlays and RGBA construction
retain their existing palettes; source transparency indices are preserved (they
are not necessarily index0). Headquarter and Advanced Reactor are already RGBA and need no PNG palette.
The engine checker now compares the palette color AND alpha of every source pixel,
not just filenames/frame dimensions. Live confirmation after restart is pending.

## Placement

`rules/scrin/supplied-building-art.yaml` loads after the historical China/Scrin2x
layer. `sequences/scrin-supplied-buildings.yaml` defines unique `.supplied` images,
inheriting the previous actor's icon, shared construction and condition overlays.
Idle/damaged/placement/death frames bind the original PNG directly. No alternate
damage artwork or animation is claimed; these are supplied static images.

Two-wide bodies use scale1; wider production/support bodies use scale1.25. Native
Bottom anchoring includes the engine's width/4 footprint allowance. Per-image upward
offsets keep the irregular roots within the front selection edges rather than
placing the entire transparent frame's bottom on a generic ground point. Selection
height follows the actual silhouette. Building dimensions, occupied/transit cells,
LocalCenterOffset, hit shapes, spawn/exit positions and refinery docking are unchanged.

Legacy terrain bibs are removed in favor of the supplied rooted foundations. GRAV's
old orb and pylon overlays are removed because the PNG already contains those parts;
condition/effect overlays are retained. No other faction artwork is changed.

`placement-review.png` shows composed native-size silhouettes and selection bounds;
`supplied-contact-sheet.png` shows the original inputs. Neither is a live screenshot.

## Validation

- .NET6 build succeeds in isolated output (no running game stopped).
- `--check-scrin-supplied-buildings` loads all actual native/damaged/placement/death
  frames plus icons/construction with the engine. Resolved filenames and Bottom
  offsets checked for every actor; tech center.png resolves on SCRT.
- `tools/test_scrin_supplied_buildings.py`: original bytes, full-alpha selection
  coverage, resolved binding, UI/construction preservation and gameplay equality.
- Historical China/Scrin2x tests retain checks on the old images and explicitly
  permit only the later art-only actor substitutions. They pass.
- Disposable faction map/water loading and existing Shard Launcher tests pass.
- Authoritative testmap1.oramap is untouched. Existing Scrin test slots pick up these
  actor bindings on a fresh launch. Live GPU/docking/production review remains pending.

Generator: `tools/art/bind_scrin_building_pngs.py`.
No commit, push or release.

## Other pending user requests

Shard Launcher25-degree/fixed-battery revision is now integrated as pass22. The
subsequently requested native Plasma Cannon is integrated as pass05. Live review
remains pending for both; see their respective READMEs.

All currently supplied China/Scrin building PNGs are integrated except the explicitly
unused foundry.png. Their live GPU/production/docking review remains pending; see
`../new-faction-buildings/README.md` for the later bindings and validation.
The unspecified Soviet buildings requiring movement toward the upper bib corner
still need identification from the user; no speculative mass repositioning was made.
