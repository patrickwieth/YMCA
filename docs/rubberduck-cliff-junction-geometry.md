# Native / vertical junction: historical geometry diagnosis

**Later compatibility treatment:** the [terrain loop follow-up](rubberduck-terrain-loop-followup.md)
adds source-authored rock abutments to the already blocked exposed feet, using
one shared runtime/editor layer. Both front orientations, blocked-foot movement,
Undo/Redo and save/reload are checked without changing terrain or saved actors.
This deliberately retains old mixed contours; it does not install either rejected
warped trial below or pretend that old maps have gained closed-family topology.

The soil-fringe fixes do not solve the mismatch between an outward-leaning native
wall and a vertical fallback wall. This investigation identifies an actual failing
section rather than treating another softened edge as completion.

## Reproducible section

Mountain Valleys, four players, seed 42; production map from
`%TEMP%/ymca-convex-apron/production/mountain-valleys-players-4-seed-42.oramap`.
Use `--rubberduck-rock-coast-playtest MAP OUTPUT cliff-review 26`.

At roof cell **87,-23**, direction 1, `plateauwall49` supplies the vertical face.
Its successor at **88,-23** also uses `plateauwall49`. The preceding staircase cells
85,-25 and 86,-24 have original straight/end/corner pieces. The exposed low dirt
includes **86,-22** and **87,-22**. The diagonal endpoint **88,-22 is ordinary,
height-zero grass**, not a reserved cliff foot.

The native planner requires the primary blocked foot and safe (+1,0), (0,+1),
(+1,+1) support before replacing this face with its complete native assembly.
The last support is absent here. Placing a full native outer closure regardless
would extend into the unreserved neighbor. Painting the blocked dirt green would
not fix this mismatch either.

For a southwest face, in screen-art coordinates relative to its height-four actor:

- Roof endpoints: `(-64,0)` and `(0,32)`.
- Native foot endpoints: `(-128,160)` and `(-64,192)`.
- Vertical foot endpoints: `(-64,128)` and `(0,160)`.

Matching the roof alone therefore does not join the feet. The follow-on repair
needs a genuine supported connection, with both roof and foot sockets, rather
than another ground-material overlay or an unrestricted native-piece replacement.

## Better diagnostics

`cliff-review[-shaded]` now enables `ReportCliffGeometry` on its diagnostic world.
It records nearby cells (tile, height, ramp), blocked-foot diamond vertices and
native/fallback actor positions through the actual renderer projection. Coordinates
are converted to framebuffer pixels using `Renderer.WindowScale`.

The first report used logical UI pixels. The tested window has scale **1.5**,
1024x768 effective resolution and a 1536x1152 framebuffer. Those initial coordinates
must not be interpreted as screenshot pixels. Corrected reports explicitly include
`CLIFF VIEW`; appended logs must be split at the latest header.

The utility also writes `cliff-junctions.tsv`, listing full-height fallback faces
with blocked primary feet and the actual support-cell types/heights/ramps. This is
a read-only constraints report, not an automatic license to unblock cells.

These diagnostics are absent from ordinary generated gameplay maps. No engine
patch, collision change or production art replacement was made in this step.

## Rejected map-local candidates

Evidence root: `%TEMP%/ymca-junction-geometry`.

- `baseline/ingame-projected.png`: unchanged production section, actual client.
- `candidate`: original end 30 plus a tapered derivative of installed native face 0,
  replacing the vertical face. A bilinear geometric mapping held the roof sockets
  and joined the native starting foot to the vertical successor. 1089 forward/inverse
  samples passed; map terrain survived save/reload. Actual rendering nevertheless
  showed a dark opening at the join and visibly distorted lower rock strata.
- `backed`: retained the old vertical face behind the candidate to test the missing
  backing hypothesis. The opening was reduced, but layering/seams and distorted
  strata remained unacceptable. This is not an accepted full-section composition.
- Both trials were fitted/warped derivatives with nearest source sampling, not raw
  original artwork. Their local provenance records the input hash and clipping.
- `rejected-code`: archived experimental geometry, utility and renderer variant.
  The experimental trait option and utility were removed from the repository after
  inspection. The candidate packages require that archived code and are not valid
  production packages for the restored renderer.
- `final-baseline`: restored renderer, diagnostic projection and read-only support
  report. Candidate terrain binaries and all ten installed cliff images are checked
  against the unchanged baseline. No new movement certification is claimed for
  the rejected visual-only trials.

**Status of these tapered trials: rejected.** The root geometry mismatch is identified; neither tapered trial
is installed or counted as a visual improvement. A production solution still needs
both orientations, supported clearance, source-art quality, shared editor behavior,
transaction tests and gameplay regression checks.
