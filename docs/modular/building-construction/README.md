# Modular construction visuals

Latest follow-up: construction is visible again for every non-Scrin faction via
`RenderSprites.AnimatedBuildingSequences: construction`. Scrin buildings/defenses
explicitly keep the empty allowance. Other old body/production animations stay
static; forward/reverse clocks and callbacks are unchanged. See
`../construction-and-native-cut-followup.md`. Generated construction sequences remain
available for Scrin's timing/controller path, but are not displayed there.

User requested retiring mismatched old building buildup art and using the existing
China construction sites by size instead. Implemented locally, no release/push.

`^Building.WithMakeAnimation` now selects `construction`, not `make`. The generated
`sequences/building-construction.yaml` provides that sequence for each resolved
building image. Footprint area selects small (1–2 cells), medium (3–9), or big
(10+), using the unchanged China PNGs and timing: five frames per stage, Tick80.
Production queue durations, footprints, costs and weapons are unchanged. The visual
construction/selling interval now follows the China stages, not the retired art.
Guard Tower and Ion Defense can now use this generic animation too. The isolated
voxel flak actor remains exempt because it has no sprite body. Trees, infantry
and other non-building make animations remain unchanged.

Only inherited `make` data is bypassed, not deleted: no original animation assets
were removed. A new sequence name avoids inherited old frames, offsets and facings.
China/Scrin sequence inheritance keys were named where overlay merging required it;
parents and their order are unchanged.

Regenerate from engine-resolved inventory after adding new buildings:

```
OpenRA.Utility ca --list-building-construction > inventory.tsv
python tools/prepare_building_construction.py inventory.tsv
OpenRA.Utility ca --list-building-construction --check
```

The last command constructs all relevant actors' trait dependencies and loads the
actual construction sprite reservations/frames. `engine-check.tsv` records the
validated image/size bindings. Utility assembly was built into the isolated
`.construction-utility` directory, without stopping/replacing the running game.
The original inventory is a historical size-selection snapshot; later footprint
refinements do not silently change approved stage families or animation lengths.
The checker now validates the actually selected stage family and its frames.

Current ground registration is supplied by the final
`sequences/construction-alignment.yaml` layer. PNG anchors include source alpha
margins, Bottom-anchor correction, final offsets and scale; legacy SHP anchors
come from resolved engine sprites. Reproduce after visual placement changes:

```
OpenRA.Utility ca --audit-building-icons audit.json
OpenRA.Utility ca --export-legacy-construction-anchors legacy.json
python tools/prepare_construction_alignment.py audit.json.sequences.json legacy.json
```

The latest local native review covers construction start, middle and completion
for ten representative actors, including legacy Swiss Bank/Grand Cannon. It also
confirms Scrin's intentionally hidden construction remains hidden. Stage timing,
reverse selling behavior and callbacks were not changed. See
`../../building-finish-review.md`. Restart through `tools/test-game.cmd` to compile
and load current changes; existing matches retain their original rules.
