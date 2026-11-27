# Handcrafted coast follow-up — Glitters / Two Sides installed locally

## Current status

The user reported remaining Two Sides beach fragments/transitions and requested a separate Dune cliff/map-edge improvement. They also reported duplicate Glitters and Two Sides entries.

Initially a running user OpenRA process locked Game/engine/bin. No user process was stopped. Build and native tests used `.tmp/map-chooser-build` and `.tmp/coast-review-engine` instead. After the user process exited independently, normal Game was rebuilt and both updated packages were installed with their matching DLL. The custom chooser exclusion now runs in the normal Game build.

## Implemented and tested

- **Duplicate chooser entries:** SkirmishSetupLogic's custom remote-map pool did not filter Unknown maps. It now uses IsSelectableMap, excluding historical/development packages. CheckActiveMapsCommand checks that exact predicate and exactly three distinct selectable handcrafted maps. Previous normal-tab tests did not exercise this custom pool. Archive packages and UIDs remain unchanged.
- **Two Sides:** derive the beach after final offshore-boulder substrates, instead of before. Grow one connected strip through eligible land; do not grow across the rock-bank barrier. Remove disconnected inherited beach pieces. Land-based boulders inside the strip retain sand underfoot rather than producing green holes. Offshore rocks retain water underneath.
- Use map-local procedural dry-material feathering while retaining authored water silhouettes. Other maps retain their original renderer defaults.
- First square-distance study produced inland sand flecks behind the rock barrier and was rejected. The final land-only flood removes those flecks. Inspected both long beaches and the central rocky strip natively; final southern view confirms the previously visible green break under the boulder cluster is gone.
- BaseTerrainOverrides, all resource amounts, zero heights, original actor initializers, and actual ground/ship movement pass again. Native runtime acceptance: `.tmp/shore-end-final/handcrafted-runtime.json`.

## Installed installation / evidence

- Production export: `.tmp/shore-fragments-production/handcrafted-two-sides-naval.oramap`
- Current Two Sides UID after the tree follow-up: `2b8754048e214a637931ee006a9b28a0efae9f5c`.
- Earlier Two Sides `e41acd5c0f3b538e1faad53c4006645a82ad749a` is archived byte-exactly, not selectable.
- Dune was unchanged during the beach/tree work. Its later requested boundary-roof update is now installed separately (see below).
- Native screenshots: `.tmp/shore-fragments-native3`, `.tmp/shore-center-native3`, `.tmp/shore-south-final` (Screenshots subdirectories).
- Package regression: `python tools/test_handcrafted_shore_refinement.py ../.tmp/shore-fragments-production/handcrafted-two-sides-naval.oramap -v`.

Installed both updated packages in mods/ca/maps and art-source/handcrafted/packages, with matching validation entries. Earlier installed versions are preserved in history/before-edge-followup and indexed under Unknown. Native chooser and package checks passed after installation. No historical title was renamed or repacked.

## Glitters contour follow-up

- Current production UID after closed-corner follow-up: `e8228bc0690ae8d344cc2712678493dc5cc79b7e`.
- Close visual Clear/Rough holes within enclosed rock roofs, instead of tracing unnecessary internal cliff faces. Retain exterior-connected areas, starts, resource cells and all 531 reclaimed construction cells. This only changes visual tiles and generated scenery; BaseTerrainOverrides remain identical to the prior installed package.
- RockCoastBody uses the actual visual roof tile mask on Glitters, not just original Rock collision. Logical-boundary clipping prevents overhanging fragments outside the playable footprint. Other maps retain existing defaults.
- Rejected removing corner caps: that exposed gaps between native wall faces. Final output retains the calibrated corner pieces.
- Inspected central and side islands, nearby open bases, and the full overview. Native movement, yard placement and all original isolated oil-access expectations passed again: `.tmp/glitters-edges-final-accept/handcrafted-runtime.json`.
- Package test: `python tools/test_handcrafted_rock_edges.py`. It compares against the byte-exact pre-follow-up archive, checks original actors/terrain/resources/heights and protects every reclaimed cell against visual roof refill.
- Final combined production export: `.tmp/handcrafted-edges-final-production/`.

## Approved-form follow-up: trees and closed corners (installed)

The user approved both overall map forms and requested only remaining corner/gap corrections on Glitters and a few trees on Two Sides. Preserve those approved shapes.

- Two Sides now has 39 decorative trees in 14 sparse groups. Placement requires broad Clear grass (nine-cell-wide neighborhood), no resources within five cells, at least twelve cells from starts and five from original actors. The inherited decoration trait has OccupiesSpace:false: no new navigation or construction blockage. Original trees/actors remain unchanged.
- At the sandy coast only, substitute the solid low native rubble cut for the cave-like dark variant. Offshore rock fields and the coastline geometry remain unchanged. Remaining small beach tips have NOT been globally reshaped; do not claim all shoreline imperfections are eliminated.
- Native side-corner source images 26/30 contained unsupported dark openings under triangular caps. Glitters uses map-local derivatives with the matching native face translated into that opening at original pixel density. Original PNGs are untouched, with provenance and exact pixel-reconstruction tests. ClosedSideFaces prevents the new facade from being clipped back to the old narrow rim. Other maps do not enable this option.
- Build/install occurred only after checking no user OpenRA process was running. Prior UIDs are archived byte-exactly in history/before-trees-and-closed-corners and the hidden lookup folder. Exactly three distinct handcrafted maps remain selectable.
- Production staging/evidence: `.tmp/trees-corners-production`, `.tmp/two-sides-trees-native`, `.tmp/two-sides-trees-accept`, `.tmp/glitters-closed-sides-native`, `.tmp/glitters-closed-sides-accept`.
- Native ground/naval movement, construction placement, original oil isolation and post-install package/chooser checks passed. Native tree and corner screenshots were inspected directly.

## Dune completed separately

Dune now continues its visual roof to the logical map boundary and uses tile-based occlusion, boundary clipping and closed side corners. Gameplay data remains unchanged; the old package is archived. See **docs/dune-factory-kirov-fixes.md** for the installed UID, native visual/movement checks and the separately reproduced Nod exit / Kirov fixes.
