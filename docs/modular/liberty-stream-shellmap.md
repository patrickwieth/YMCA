# Liberty stream variant — work in progress

`mods/ca/maps/liberty-stream-shellmap/` is now the sole active menu shellmap
(`Visibility: Shellmap`), following explicit user authorization to integrate it.
Installed-runtime preflight passed before activation. The old Liberty map is
retained, disabled; prior flags are backed up in
`art-source/liberty-shellmap/previous-stream-selection/`. The Liberty activation
script (also used by `liberty-shellmap.cmd`) now selects this stream variant.
Existing running games were not stopped; restart to load the new menu map.

Regenerated the variant using the compact rounded native-cell island from
`PrepareLibertyShellmapCommand` and the current Python layout: six flaks around
the statue and ten cruisers around the island, sorted front-to-rear relative to
the incoming bombers. Engine preflight passed for all building footprints,
water placements, sprite loading and bomb ranges. An isolated 3500-tick run
passed with kills on both sides and no Lua errors. This is not a completed
visual review or long-duration breakthrough balance test.

The variant Lua already stops bomber spawning, flak reconstruction and cruiser
replacement when the statue dies; the statue itself is not reconstructed.
Existing units remain in play. Ships propagate vacancies towards the rear.

Recurring Kirovs now use a 14-second interval in `Game / isometric`, at the user's
latest request. The opening 18-airship formation and live cap are unchanged.
The approved release preparation also aligns `Game-master` and `Game-release`
to this 14-second interval.

The Aegis turret alignment is corrected in `rules/misc/ships.yaml`: the aft socket
is now `-680,0,0` instead of `-1000,0,0`, and classic square-grid perspective
compression is disabled for this actor. This keeps the turret on the rear deck
in side views without pulling it forward in end-on views. Original ship/turret
sprites, sequence scales, weapon definitions and turret speed are unchanged.
The AI variant inherits the correction. Native before/after and full-rotation
renders were inspected; evidence and the test fixture are under
`art-source/aegis-turret-alignment/`. The comparison has old/new alternating rows;
the final sheet's top four rows cover all 32 hull facings and bottom four cover
all 32 independent turret facings. Only the test-derived actor disables idle
realignment, so production targeting behavior stays unchanged. Test clients used
isolated support directories; the user's game was not stopped.

Pending: inspect the actual in-game island/shoreline and offscreen stream entry,
verify sustained breakthrough timing and rear-ship movement. Do not mistake the
older liberty-shellmap.md completion status for this variant's status.
No release, commit or deployment performed.
