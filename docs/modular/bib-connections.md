# Dynamic foundation links and Soviet joint remap

Local implementation in `Game`, branch `isometric`. No release, push or branch switch.

## Behavior

`BibConnectionLayer` draws 1x1 foundation tiles across exactly one empty cardinal
or diagonal cell between two real building bibs belonging to the **same player**, not merely
allied players. Each cell is drawn once. Generated links never act as sources.
Removal, destruction, conditional bib visibility and ownership changes invalidate
the links on the next simulation tick/render. Placement of a new building is also
picked up automatically. Mixed-owner crossings are not claimed by either owner.

These are renderables, not actors, terrain replacements or occupied cells. They
cannot expand the build radius, block movement, affect exits or change gameplay
footprints. Existing building occupancy, map bounds, allowed land terrain and
height boundaries are checked. Moving units can drive over the decoration without
making it flicker. Links are not rendered using hidden enemy building state, and
are hidden in fog; they do not have their own persistent frozen-under-fog actor.

Supported source families: Allied, Soviet/China, GDI, Nod and Scrin.
China now explicitly opts into the Soviet fixed grid and owner-color masks at the
user's request; see [China bibs](china-bibs.md). Source geometry follows resolved overlay world
offsets and sprite registration, including raised Nod/Scrin top planes. A link
uses the first stable source actor's faction artwork. Player-color accents are only used when the source's `WithBibRemap` matches
that foundation family. Soviet joints use `bibplayer`; the later Allied black-rim
extension uses `alliedbibplayer` (see [Allied rim masks](allied-bib-remap.md)).

### Existing alignment exceptions

The renderer refuses to guess connections for bibs not aligned to the actual
cell grid. The current geometry audit identifies `fcom.checkpoint`,
`fcom.checkpoint.husk` and `tpwr` as such pre-existing exceptions. Their art and
positions have **not** been silently moved. Their lines can still be remapped
where configured, but they do not serve as link anchors.

## Soviet-only color accents

`WithBibRemap` is inherited through `^Soviet1x1Bib`. It adds a joint-only indexed
mask to the existing actor's RenderSprites collection, using the exact same
position and enable condition as its Soviet bib. Standard owner-palette updates
handle capture. Placement previews include the mask as well.

The expanded mask includes the original six grey joint shades, black edges and
additional joint shadow/highlight shades. Extra shades are selected only next to
an authored line, avoiding unrelated chips. Concrete, brown dirt and transparency
are not remapped. Non-black joints retain the approximately 65% blend. Black edges
are now **fully opaque** player-color replacements with a 128/255 source luminance:
the earlier 72/255 tint at 65% opacity still looked black in game. Per-index alpha
lives in the PNG palette (0 transparent, 7 opaque, other joint indices 166/255),
with premultiplied RGB as required by OpenRA's player remapper. There is no second
global alpha multiplier. The original black base pixels are retained underneath,
but cannot show through the opaque replacement.

Soviet now uses a `grid` sequence: larger bibs are composed from integer translations
of the native 128x64 1x1 tile, giving fixed 64x32 half-cell panel spacing. No stretching
or per-building rescaling. Thus lines meet at building/filler boundaries, including
mixed sizes. Original canvas dimensions, alpha silhouettes, scale and offsets are
retained. China now also uses the `grid` sequences. The kennel explicitly
receives the Soviet grid/remap too; it did not inherit the shared bib template.

The five original `bits/soviet/buildings/bib*.png` files are unchanged. Derivatives
live in `bits/soviet/buildings/remap/` (masks) and `grid/` (fixed-grid bases), generated
by `tools/art/generate_soviet_bib_remap.py`, with source and output hashes in
`remap/manifest.json`. Original idle assets remain unchanged. China explicitly selects the generated
fixed-grid assets and player-color accents; supplied originals are not overwritten.

## Validation and review

- Mod assembly builds successfully.
- `Modular.Tests/BibGapResolverTests.cs`: all eight gap directions, width/off-axis rejection,
  ownership, occupied cells, source removal/capture, no propagation, crossing
  deduplication and contested owners.
- `python tools/test_soviet_bib_remap.py`: source hashes, exact selected pixels,
  transparency, indexed palette, preserved silhouettes, black-edge remapping and
  identical native half-cell joint samples across all bib sizes.
- Engine utility `--check-bib-connections`: resolves native connector sprites,
  all five fixed-grid bases and indexed masks, identical mask/base placement, trait construction and
  Soviet-vs-other-faction bindings.
- A separate live game exercised initial connections, source removal, capture,
  reconnection, new actor placement, actual building destruction and diagonal
  source removal. The final
  successful result is in `art-source/bib-connections/native-lifecycle.txt`.
- Native screenshots were inspected: the strips join the existing foundations,
  red/blue accents remain confined to joints, and the removed building leaves no
  connector behind. Capture changes the remaining pair from blue to red.
  [Before](../../art-source/bib-connections/native-before.png) /
  [after removal and capture](../../art-source/bib-connections/native-after.png).
  Earlier screenshots and masks are preserved as `pass01-*` / `remap-pass01/`
  and `pass02-*` / `remap-pass02/`. Current screenshots include the opaque
  player-color fix for black edges; fixed-grid base hashes are unchanged.

The opt-in `BibConnectionTestProbe` is only enabled by the diagnostic map under
`.art-validation/bib-connections/`; it is not part of normal world rules. It
intentionally removes/captures/spawns/destroys its test buildings. Only the
assistant-started test process was closed.

The broad existing YAML lint is not clean (unrelated prerequisite errors). The
older `--check-soviet-bib-alignment` also expects an obsolete construction-yard
body offset; it is not used as proof of these changes. Current resolved geometry
was exported and independently audited instead. No building source pixels,
footprints, exits, weapons, or unrelated faction palettes were changed.

Restart the game executable before testing the new C# traits; an already-running
process will still have the old assembly loaded.
