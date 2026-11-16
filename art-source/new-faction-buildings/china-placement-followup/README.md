# China supplied-art placement follow-up

User requested weapons factory further right, propaganda tower higher on its bib,
and a larger Internet Center bib. Installed locally:

- CHWEAP: Bottom16,2 instead of0,2;16 native screen pixels right. Both visual
  selection envelopes translate by16 pixels. Bib, building footprint and exits stay put.
- CHPROPAGANDATOWER / PDL: Bottom0,-9 instead of0,-1;8 pixels up. Recomputed visual
  height76 and support/PDL origins. Explicit PDL selection override also updated;
  it had retained legacy bounds/height through an inherited override.
- CHINTERNETCENTER: original Soviet4x4 bib instead of3x3. No building scaling,
  relocation, occupancy, economy, production or weapon changes.

Also completed the preceding supplied bunker integration: bunker.png and fortified
bunker.png bind directly at Scale1 / Bottom0,12 and0,21. Both use the existing2x2
Soviet bib. AI/advanced-infantry variants resolve their proper supplied image; their
cargo, cost, health, minefield and all other gameplay remain unchanged. Baselines
are under ../china-bunkers/.

`engine-check.txt`: isolated engine sprite loading succeeded for the complete binding
set, including all bunker/tower variants. The checker now verifies X as well as Y.
Direct-binding and gameplay-isolation regressions pass. Targeted selection/gameplay
checks for the nine affected actors pass. The broad historical source/selection suite
is NOT fully green: it still has the pre-pass30 Shard hash, and newly replaced China
power-plant art differs from its old calibration (ordinary power plant exceeds its
old selection envelope). Those images were not overwritten or silently rebaselined.
Accidental generator-wide recalibration of the two reactors was reverted; their
previous runtime registration remains unchanged pending separate review.

The generator accepts an actor whitelist for targeted changes:
`python tools/art/bind_new_faction_buildings.py chweap chpropagandatower chpropagandatower.pdl chinternetcenter`

`placement-review.png` is a composed native preview, not a live game capture.
Live appearance/effect origins still require review after restart/new match.
No process was stopped, no runtime DLL overwritten, no commit/push/release performed.
