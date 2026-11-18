# Allied black-rim player-color masks

Local work in `Game / isometric`, separate from `Game-master`'s dedicated-server
fix and Kirov shellmap timing.

Only opaque **pure-black** pixels in the five supplied Allied bib PNGs are masked.
The mask is fully opaque and uses a 128/255 grayscale remap source, so black is
replaced by a visible dark player color rather than showing through a translucent
overlay. Non-black pavement, cracks, interior details, alpha silhouettes and all
building sprites are unchanged. This does not apply the Soviet fixed-grid pattern
to Allied artwork.

Original runtime base filenames are unchanged. Derived masks are explicitly
registered as `remap` sequences under `alliedbib1x1`, `2x2`, `3x3`, `4x3`, `4x4`.
`tools/art/generate_allied_bib_remap.py` creates `bits/allies/buildings/remap/`,
including a source-hash manifest. In particular, the supplied 447x224 4x3 canvas
and its fractional engine anchor offset are preserved exactly.

`WithBibRemap` now has a configurable image-family prefix and player palette;
its defaults preserve Soviet behavior. Allied visual templates and the explicit
factory/conyard/Grand Cannon/fake-building bindings use `alliedbibplayer`.
Actor render collections handle construction conditions, captured owners and
placement previews. Dynamic connector tiles use the same source-family palette,
but only Soviet connectors use the Soviet `grid` sequence. Unrelated families
are not recolored.

Validation:
- Mask tests check exact coverage of every opaque black pixel, zero coverage of
  every other pixel, identical dimensions, full opacity and unchanged source hashes.
- Soviet mask regression tests pass unchanged.
- `--check-bib-connections` resolves all ten foundation/mask pairs in the engine,
  checks sprite offsets/scale, actor construction and faction palette bindings.
- An isolated live game tested all five Allied sizes, red/blue/green rims,
  diagonal/cardinal connectors, source removal, capture, placement and destruction.
  Native before/after frames were visually inspected: pavement detail remains
  untouched and the blue pair changes to a red rim after capture.
  See `art-source/allied-bib-remap/native-before.png`, `native-after.png` and
  `native-lifecycle.txt`.

## Installation state

At validation time the user's existing OpenRA process held the runtime DLLs open.
It was **not stopped**. The updated CA assembly was built and tested separately in
`.art-validation/allied-bib-remap/runtime/bin/` using copies of the current engine
binaries. The new rules/assets are in the worktree, but the normal runtime CA DLL
still needs replacement after the game closes; do not treat an ordinary restart
with the old DLL as a completed installation.

After the game is closed, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .art-validation/allied-bib-remap/install.ps1
```

The installer refuses locked targets, backs up the old mod DLLs and copies only
the staged CA/Cameo DLLs and debug symbols. During the v0.97.2 preparation these
staged assemblies were refreshed from the validated isometric index, including
the dedicated-generator changes. It neither stops nor starts the game. Isometric source commits/pushes were
subsequently authorized; these foundation-art changes remain separate from the
stable v0.97.2 release.
