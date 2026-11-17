# Local visual-feedback follow-ups

## Nod Power Plant

User reported body far too high on the bib. Supplied PNG is256×256 but its opaque
bounds end at y189; the Bottom anchor included the transparent lower padding.
Moved idle/damaged from0,-15 to0,35, preserving source pixels, footprint and bib.
Composed layout in art-source/nod-power-alignment/before-after.png was reviewed.
User then requested slightly higher: **current offset0,27**, eight pixels above
that first adjustment (42px below the original). The saved comparison shows the
first adjustment, not the latest8px refinement. No artwork edits.

## Allied flak

AGUN now uses four clockwise firing mouths rather than two diagonal slots:
529,98,1500;529,-98,1500;622,-98,1287;622,98,1287. Existing single-shot ZSU-23
cycles the slots automatically; damage/reload unchanged. Disabled classic fudge;
engine checker now uses the actual128×64 grid and validates all128 barrel/facing
pairs (maximum0.177px). CRAM overrides remain isolated and pass its checker.
Original approved pass15 pixels unchanged. Live confirmation pending.

## Scorches/craters

Confirmed palette mismatch: legacy RA sc*.tem/cr*.tem used isotem.pal via terrain.
That gave blue/purple scorches and pale green crater colors. Dedicated legacy-smudge
palette now uses RA temperat.pal with original shadow indices3,4 on the three
isometric temperate tilesets. Six other tilesets retain their respective previous
palette files/shadow indices under the new name. All four smudge layers use it;
flame/smoke palettes, weapons and the terrain palette are unchanged.
Engine loaded all9 palettes and checked the isometric entries against source RA
colors. Engine-exported color comparison in art-source/scorch-palette-review/ was
reviewed; it is not a live screenshot and does not reproduce the full terrain.

## Chinook rotor registration

Voxel body keeps its existing0.65 scale and camera pitch125. Unlike native sprite
buildings, this fixed-pixel voxel projection is not a2:1 camera, so simply halving
forward offsets would still misalign other facings. A Chinook-only
WithVoxelRotorOverlay scales the post-rotation world X by tile-height/tile-width;
this reconciles pixel-based voxel coordinates with rectangular world projection.
Runtime and actor preview both apply it. Rotor centers come from actual chinook.vxl
pylon top surfaces: forward(30.5,0,15.5), rear(-39,0,19.5), at existing0.65 scale.
Offsets449,0,164 and-574,0,206 apply to both airborne/grounded animations. TRAN,
TRAN.Eagle and TRAN.paradrop inherit the fix; other dual-rotor aircraft unchanged.
Engine check inspects source voxel surfaces and32-facing projections for all three
variants (maximum0.092px). Body profile was inspected; full live rotor review pending.

## Validation / pending

Compiled successfully and initially installed the gameplay DLL. A subsequent
checker-only build copy was blocked after the user started OpenRA; used an isolated
binary copy with the freshly compiled CA DLL instead. No running process stopped.
--check-rotor-and-smudge, --check-allied-flak-art and --check-gdi-aa-art pass.
Normal building-test launcher rebuilds after the game is closed. No commit/push/release.

User also reported unspecified Soviet buildings off-center on bibs. Requested names
or screenshot; no speculative mass movement of approved building placements.
