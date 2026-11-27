# Handcrafted maps — implemented and locally installed

Active development worktree: **Game**. Game-master is reserved for releases; the earlier placement there was corrected.

All three production (non-probe) packages are installed in `mods/ca/maps/` and appear under **Handcrafted Maps** (also Conquest/Tactical). They are not published or deployed. They require this updated engine/mod build, not an `.oramap` copy into an unmodified v0.97.6 client/server.

| Map | Original starts | New UID |
|---|---:|---|
| All that Glitters – Barren | 8 | `e8228bc0690ae8d344cc2712678493dc5cc79b7e` |
| Dune Patrol | 2 | `5577395023c6f50282673e70fa16f70a954ea49d` |
| Two Sides Naval | 6 | `2b8754048e214a637931ee006a9b28a0efae9f5c` |

Files: `handcrafted-all-that-glitters-barren.oramap`, `handcrafted-dune-patrol.oramap`, `handcrafted-two-sides-naval.oramap`. Reproducible staging copies and native validation receipts are under `art-source/handcrafted/packages/`.

## Preservation

- Original pre-isometric reference revision: `a8559948636b5b499b0f9c8a9a1364dc2c1e2066`. Byte-exact original packages/files and SHA-256 manifests remain under `art-source/handcrafted/originals/`.
- Translate logical CPos cells, then compute isometric MPos; do not stretch old storage coordinates. All 29,100 original playable cells, raw resources/densities, flat elevations and original actor initializers/IDs are preserved (only Location coordinates are translated). The user's subsequent Glitters adjustment deliberately converts 531 original Rock cells to Clear; all other base terrain classes remain unchanged. The exact cells are recorded in packages/validation.json.
- No building footprints, costs, unit balance, generator dimensions or live tournament state changed by this work. Glitters' added construction terrain is an explicitly requested map-layout change, not a footprint adjustment.
- Existing replay packages, map IDs, downloads and Discord URLs are untouched. New maps have new UIDs. This is not a migration of historical tournaments.
- Original resource indices are retained in map.bin. Runtime uses the already-existing mod policy normalizing old ore/gems to Tiberium; this work does not invent a new resource economy. Map previews display the corresponding current resource colors.
- Dune Patrol remains a two-start map. No fabricated four-player variant. Original start-slot selection on the larger maps still matters for tournament balance.

Further installed Glitters contour / Two Sides beach cleanup and the custom chooser archive-exclusion fix are documented in **docs/handcrafted-shore-followup.md**. Dune's requested cliff-to-map-edge adjustment is now installed; see **docs/dune-factory-kirov-fixes.md**.

## User-requested refinement after native review

- **Dune Patrol:** its earlier approved package is preserved byte-exactly in history. The subsequently requested visual roof extension to the map edge is installed under a new UID, with unchanged gameplay terrain/resources/actors.
- **Glitters:** reclaim one boundary layer of oversized blocked rock, preserving cardinally connected rock spines rather than introducing diagonal-only point contacts. Resource-bearing cells and all isolated oil pockets are protected. 531 of 1,406 formerly blocked Rock cells now use buildable Clear terrain. Actual movement and yard placement pass again; none of the five oil pockets gained a ground route.
- **Two Sides:** remove the raised dirt roof and the second land-facing ridge. Use low, source-derived rock bank transitions directly between grass and water. RGB comes only from bounded native cliff-face cuts, with alpha feathering; no original PNG was altered. Beaches have a continuous material strip with an authored land transition; old square-corner autotile pieces are excluded on this map. Its base terrain, water connectivity and actor placements remain unchanged.
- Failed rear-face offset and repeated-boulder-bank studies were rejected during native review; those are not the installed result. The unused inset renderer modification was removed.
- Earlier production packages are preserved under history/before-coast-and-space-refinement. The two replaced UIDs are also indexed invisibly from mods/ca/development-maps for historical lookup; they do not return to the normal map menu.
- Updated native overviews, close-ups and movement checks are stored in validation/. Neither map includes review or profiling traits.
- The separately reproduced full-reveal performance issue and its improvements are documented in docs/map-reveal-stutter.md.

## Engine support (optional, no new metadata on ordinary maps)

`tools/patches/handcrafted-map-geometry.patch` is registered in the shared Windows/shell patch list:

- `CellBounds`: flat logical-cell playable rectangle inside projected bounds. Contains, gameplay clamp, random cells/edges and edge distance exclude unused corners, including flight destinations. Raised terrain is rejected for this mode.
- Projected-coordinate viewport clamping remains rectangular: clamping rendering corners to the logical diamond initially cropped the scene; native review caught and corrected this.
- `BaseTerrainOverrides`: persistent gameplay terrain independent of visual tile art. Dynamic resource/bridge terrain still wins; depletion restores the original base class, not a different visual-tile class.
- Only handcrafted maps opt in. Export regression verifies an ordinary flat map retains normal Contains behavior and gains neither field when saved.
- `HandcraftedMapSupport` validates that the matching engine fields exist and provides correct legacy-resource minimap colors. An old mod build cannot silently treat these packages as ordinary rectangular maps.

Rubberduck terrain definitions add unused-by-default Rock, Rough, Beach, River and Tree classes. This avoids incorrectly turning River into navigable Water, Rock into a differently traversable Cliff, or unbuildable Rough into buildable Clear. The tileset generator preserves these entries on regeneration.

## Visual implementation and review

- Current Rubberduck ground/water and coherent flat rock boundaries; all stored terrain heights and ramps remain zero. Decorative cliffs do not introduce elevation/projectile barriers.
- Small isolated Rock components are represented by intact boulder cuts from the supplied Rubberduck atlas, not repeated tiny rectangular plateaus. Provenance and exact pixel-cut checks accompany the four PNGs. Originals are unmodified.
- Original biome-specific tree/husk SHP filenames and palettes are retained per map, with presentation scale adapted to the new grid. Native review found and fixed incorrect purple/white vegetation from the isometric terrain palette. These are rendering-only overrides; actor footprints are not overridden.
- Scenery remains at its original logical positions. Terrain override checks guarantee visual roof joining, water beneath rocks and material transitions do not alter collision or buildable terrain.
- Actual native desktop overview AND close-up renders were inspected. Final overview captures and runtime results are in `art-source/handcrafted/validation/`. Review-only camera/probe traits are absent from production packages.

## Gameplay acceptance

Native runtime probes completed on all three maps:

- Engineer, jeep and medium tank pathfinding between all pairs of original start areas, with current actors/terrain loaded; real units also completed their routes.
- Real destroyer movement on a native water-body route in Dune Patrol and Two Sides.
- All 14 existing construction-yard types have a legal placement near every original start (within five cells), without changing their footprints.
- All Glitters' five oil derricks have no normal ground approach from any of its eight starts **already in the original terrain**. Static original checks and native engineer checks confirm that this isolation is preserved. Do not open paths or move the derricks as a supposed conversion fix.
- Test units use separated destinations and are removed after arrival so the fixture itself does not block the next unit; an early crowded fixture was corrected, not the gameplay map.

Export/reload validation also checks exact playable membership over the whole stored map, native coordinate roundtrips, terrain classes (including the explicit Glitters reclaim list), resources, zero heights/ramps, clamping/random edges, border-ray semantics and resource-override restoration. Source/packaging tests independently verify the original files, every converted cell, original actor data, new UIDs and absence of debug/probe traits.

Validation repeated in Game after relocation: 148 engine tests passed; 20 handcrafted Python tests passed; Windows engine-patch fresh/repeat/conflict tests passed. `--check-active-maps` passes with the three installed maps and legacy exclusion still active. Test counts are engineering evidence, not a substitute for the native visual review above.

## Reproduce

```sh
python tools/prepare_handcrafted_sources.py
python tools/convert_handcrafted_layouts.py
python tools/prepare_handcrafted_boulders.py
sh tools/prepare-engine-patches.sh
# Build engine + CA + Cameo assemblies; set ENGINE_DIR and MOD_SEARCH_PATHS.
dotnet engine/bin/OpenRA.Utility.dll ca --export-handcrafted-maps art-source/handcrafted art-source/handcrafted/packages
# Copy only the three handcrafted-*.oramap packages to mods/ca/maps.
python tools/test_handcrafted_packages.py -v
```

For isolated native acceptance, export to a separate directory with `review` or `detail`, then run `tools/test-handcrafted-map-runtime.ps1 -MapPackage ... -OutputDirectory <fresh-dir> -WaitForMovement`. It starts/stops only its own process, uses a separate support profile, and does not stop the user's game. Never install review packages in the normal map list.

A live preservation spot-check of `/replay/M0071/download` returned HTTP 200 and the unchanged original SHA-256 `03939830bb9b0419fbdfde4b9d0b344ef3b83fb836cd31b9805b1fb097ef473d` (517,885 bytes). No remote files were modified.

All three native movement/placement probes and overview renders were repeated successfully using Game's own engine, mod rules and art after relocation. Building PNG hashes were unchanged. The accidental Handcrafted and map-menu development copies were removed from Game-master and its release build restored; its pre-existing Airship/direct-connect changes were preserved.

Unrelated Airship/direct-connect fixes, tournament bracket/UI work and generator rebalance remain separate. No commit, push, release or server deployment was performed for these maps.
