# Latest building art / selection / occupancy follow-ups

## Implemented locally

- Latest GDI War Factory correction (`WEAP.TD`): entire3x3 body at x-1..1,y-1..1
  is impassable. Resolved4x4 bib mask `xxx= xxx= xxx= =+==`; front/right bib
  margins and existing exit(0,2) remain passable. Art, actor origin and exit unchanged.
  Supersedes the earlier seven-solid-cell mask. Exact regression and engine occupancy
  checks pass in the isolated runtime.

- GDI missile tower: pass12 native PNG base with the removed right-side turbine area
  restored using actual source wall/paving cuts, plus the true voxel turret on ATWR.
  Direct authoritative runtime bindings, exact source palette, registered muzzles.
  See `art-source/gdi missile tower/README.md`. Not merely an asset copied to disk.
- Soviet Chemical Tower: supplied PNG completed at its clipped top/bottom with native
  source pieces, TTUR binding, no visible rotating turret. Existing firing controller
  and weapon retained. See `art-source/soviet chem tower/README.md`.
- Soviet Radar (`DOME.Soviet`): lower-left edge cells(-1,2) and(0,2) now solid.
- Soviet War Factory (`WEAP.Soviet`): full upper-right2x4 region solid:
  `xxxx xxxx ==== ====`, origin(-1,-1). Existing exit1,2 unchanged/reachable.
- Soviet Service Depot (`FIX.Soviet`):4x3 bib, `xx== xx== +===`. Upper2x2 solid;
  only repair/service cell(0,2), on the striped platform, is transit-only. Resupply
  uses that service tile. No manual dock-offset or repair-rate change.
  Native448x224 bib is a binary crop from bib4x4, with Offset32,-16 to keep every
  retained pixel and the rear corner stationary. Actor center compensated using
  Building.LocalCenterOffset(-362,362,0). Body/effects/selection placement unchanged.
- China War Factory (`chweap`): decoration footprint4x2, not the old square3x3 box.
  DecorationOffset(-441,362,0), height142; original interaction Bounds4344,4344,407,0
  and all occupancy/exits remain unchanged. Native overlay reviewed in
  `art-source/china-war-factory-selection/after.png`.

## Small required engine correction

IsometricSelectable formerly rejected DecorationFootprint when interaction Bounds
existed, and rendering would prefer those Bounds over the visual footprint. It now
allows independent mouse Bounds and explicitly bypasses them for the decoration when
a DecorationFootprint is specified. DecorationBounds still cannot coexist with an
explicit footprint. The geometry exporter follows the exact same choice. Existing
actors without this combination keep their previous behavior.

This is required to fit the user's4x2 visual box without silently changing mouse
interaction. It does NOT change building collision, production, movement or stats.

## Validation and current installation status

`.art-validation/current-building-followup/` contains current isolated build/runtime,
rule/sprite/muzzle checks, resolved geometry and grid audit. Native retained bib pixels,
correct repair destination, exact masks, ATWR/TTUR full nonvisual rule snapshots and
source-pixel provenance are covered by `tools/test_current_building_followups.py`.
The position-feedback regression checks account narrowly for the approved depot bib.
All113 decorative bibs remain grid-aligned.

The normal Release build reached the copy step but was blocked by running OpenRA
holding `engine/bin/OpenRA.Mods.Common.dll` (MSB3021). No game/Blender was stopped.
Data/art bindings are installed locally; the latest Chinese selection code is tested
in the isolated runtime, NOT copied into the running game's locked DLL. Close the game
normally and run `test-gebaeude.cmd` to rebuild/start with all new code and data.
Do not call the blocked normal build successful or claim a live play check.

The engine voxel readers and full sprite set load pass; these are not live GPU, firing,
driving or repair replays. Original supplied art and testmap1.oramap remain protected.
No commit, push, release or deployment.
