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
Live placement/construction/selling review is still pending. Restart the game to
load the changed YAML; existing matches retain their original rules.
