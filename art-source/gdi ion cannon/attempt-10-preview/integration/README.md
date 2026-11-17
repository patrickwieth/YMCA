# Ion Defense — V10, native160 integration checkpoint

Superseded by the [V11 centered bearing/turret and 2x2 bib](../../attempt-11-centering/README.md).
The turret PNG remains exact V10; base/attachment/bib details below are historical.

The user approved installing the latest GDI-gold / brighter straight-conductor /
refined-contour version. Installed the **160px candidate shown in the main rotation
and before/after previews**, not the oversized V5 and not the 192px alternative.

- Existing `Ion_Defense`, image `ion_defense.native`, same production entry.
- Separate base: one 160x160 frame; turret: 32 native160 frames, 1280x640 sheet.
- Source pixels are exact copies of `../160/`; only sheet layout and embedded PNG
  FrameSize/FrameAmount metadata are added. Engine frame i uses source (20+i)%32.
- Scale 1. Base/idle offset `-7,-24`; turret offset `-1,-10`.
- Visual selection bounds `1901,1901,0,0`, height 66; all-view requirement 64.25px.
- Turret world offset `0,-136,317`, local muzzle offset `1631,0,675`, CameraPitch85.
  **Physical projectile origin is changed** from oversized V5 to match this smaller
  art. It is not merely a selection-box adjustment. Range/damage/cost/HP/power,
  production, turn speed, collision/mouse bounds and occupied 2x2 cells stay intact.
- Original muzzle, icon and effect sequences remain inherited.
- The existing map Actor84 at `169,-35` uses it; source map unchanged.

Release build and actual engine loader pass: 1+32 native160 RGBA frames. Model vs
engine muzzle checks over all 32 directions pass, max **0.609 native pixel**.
Integration tests compare all packed frames to the approved native images and the
resolved actor against the original gameplay baseline. Art regressions, Guard
Tower, GDI-art and selection checks pass. Live GPU appearance/firing is pending.

`../export/` is the authoritative installed PNG export; `binding.json` records its
hashes and offsets. Preview generator reports remain reports of non-installing
steps; this integration is the separate explicitly approved installing step.
Historical V5 exports are preserved and hash-tested. Preview generators now compare
active assets before/after their run rather than requiring V5 to remain installed.

Reproduce from Game:

```sh
python tools/art/integrate_ion_defense_v10.py
python -B -m unittest discover -s tools -p test_ion_defense_art.py
```

No running game was stopped/restarted. Launch `Game/test-gebaeude.cmd` to reload the
art. No commit, push or deployment was made. Guard Tower, original artwork and the
protected Nod conyard remain untouched.
