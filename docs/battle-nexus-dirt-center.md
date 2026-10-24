# Battle Nexus: non-buildable resource center

The entire traversable `ContestedCenter` area now exports as Dirt. Mountain dividers, entrances, starts and planned initial resource placements are unchanged. Dirt remains excluded by building placement rules after resource depletion.

The subsequent agreed [Tiberium migration](customize-topology.md) replaces the former Gems with BlueTiberium and the central generators with blue Tiberium trees. Both green and blue Tiberium are permitted on Dirt in the game and editor. The earlier open question about ordinary ore mines on Dirt is resolved by that migration; no Ore generation remains in the shared resource setup.

A regenerated Battle Nexus map is required for the full-center ground change. Existing map packages are not rewritten: legacy resources normalize in memory and legacy mine actor IDs remain compatible tree aliases.

Evidence: `%TEMP%/ymca-nexus-center` contains the earlier 12-case full-Dirt export checks and eight-other-preset byte comparisons before the global resource conversion. Current `%TEMP%/ymca-customize` evidence covers native and legacy Tiberium client loads (506 eligible central blue cells), resource-layer extraction of both Tiberium kinds, blue-tree replenishment, non-buildable depleted Dirt, and legacy editor Undo/Redo/save/reload. Extraction probes use the same resource-layer API, not a complete harvester travel/delivery test. The later Tiberium-only batch timed out after five cases; it is not claimed as a fresh complete 12-case pass.

Tiberium retains its existing price/density/growth rules; the resource conversion is not a claim of unchanged economic balance. See the linked matrix for the independent Customize center-buildability override.
