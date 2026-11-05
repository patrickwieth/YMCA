# Hill Fortress: terrain continuation beyond the map edge

This follows [raised team bases](hill-fortress-team-plateaus.md).

The outer closed contours now sit in **16 cells of non-playable backing terrain**. The raised roofs continue across all four visible map edges; the valley cliffs remain visible. The requested playable dimensions do not change (e.g. 4P auto remains 96×96). The terrain allocation is larger, not the playable area. Existing saved maps are not rewritten.

`MapPlan.PlayableInset` survives cloning. Export bounds crop to the requested rectangle, and `ContainsPlayable` accounts for roof projection when validating routes. Starts, their full build disks, fields, generator footprints and gameplay actors remain inside playable bounds. Strategic checkpoint counts use playable dimensions, not backing dimensions; the backing cannot be used as a route around the battlefield.

The approved wider ramps remain recessed into the roof: five drivable lanes instead of three lanes plus two projecting retaining strips. Adjacent plateau terrain retains the slope; no invisible guard collision replaces the removed strips. Other presets retain their old ramp construction.

Topology recovery includes physical roof/foot tiles outside playable bounds so the generator and editor can preserve the complete closed contour. This does **not** change the engine's movement boundaries. Artwork is unchanged.

## Evidence

`%TEMP%/ymca-hill-open-edges`:

- All **24 Hill Fortress cases** in `matrix-final.log` passed twice-generated terrain/economy checks: Tactical/Operational/Strategic, 2/4/8/16 players, seeds 42/120999725. Added checks cover requested playable dimensions, five-wide ramps, elevated boundary coverage and no resources/gameplay actors stranded in backing terrain.
- The combined 32-case command reached its timeout during later Battle Nexus regressions, not during Hill Fortress. Do not describe this run as 32 completed cases. All **28 written packages** subsequently passed the independent closed-socket audit, plus its 180 general contours, 543 corrupt rejections and 52 protected feet.
- Actual normal overview and shaded movement run: no peripheral cliff ring. Infantry and heavy tanks traversed the **outermost new ramp lanes** in both directions, through all four heights; MCV deployment passed.
- Actual Strategic 4P42: tanks from all four homes visited all three checkpoints; complete at tick 2281.
- Actual editor on a resource-free generated fixture: grow stroke through the real picker, exact Undo/Redo and full terrain/resource/actor save/reload passed. Resource-filled boundaries remain protected rather than clearing ore to permit a test edit. The diagnostic now also tries wider strokes when a single-cell protrusion cannot fit native corners.

Additional checks: six actual patch-economy exports (Operational, 2/3/4/8/12/16 players, seed 43) passed with `Resources: Patches` verified in each report. Continents, Two Sides, Water Basins and Archipelago 4P42 retain byte-identical `map.bin` and `map.yaml` against the previous installed baseline. Isolated-output Modular tests passed 180/180.

The CLI previously parsed `resources=...` and then overwrote it with Hill Fortress's Glitter default. Explicit resource choices now take precedence for this preset; ordinary wizard/default generation remains Glitter.

No new multiplayer, broad performance or universal seed/balance certification is implied. Continents/Archipelago coastal fragmentation is a separate pending issue.
