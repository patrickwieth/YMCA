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

Pending: inspect the actual in-game island/shoreline and offscreen stream entry,
verify sustained breakthrough timing and rear-ship movement, correct and visually
check the reported Aegis missile-turret alignment. Activation is complete, but
that visual correction remains outstanding. Do not mistake the older liberty-shellmap.md completion status for this
variant's status. No release, commit or deployment performed.
