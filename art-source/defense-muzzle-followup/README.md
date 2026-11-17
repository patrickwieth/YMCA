# Live-feedback follow-up: muzzle origins and invisible Sentry flash

Local Game/modular changes only. No artwork regenerated, no gameplay-stat changes,
no stopped processes, commit, push or release.

## Sentry

User reported that neither alternating flash was visible. Root cause: both20ms
frames could finish in WithMuzzleOverlay's first40ms update, clearing visibility
before rendering. Changed only the dedicated muzzle-mg Tick20→40 (two frames,
80ms total). Shared left/right phase and four ground Gatling stages remain intact.
The engine checker now runs the actual Animation.PlayThen/Tick with a headless
sequence fixture: survives tick1, expires tick2 (the fastest firing interval).
Previous checker only verified frame loading/positions and missed this lifecycle bug.

## GDI AA / Ion Defense / Nod Laser

All three had the same incorrect64px horizontal-grid assumption in their old
calibration checks. Actual runtime grid is128×64. Disabled classic perspective
fudge for these three actors and recalibrated forward offsets:

| Actor | Previous | Current | Maximum engine error |
|---|---|---|---|
| CRAM (all4 stages) |1303,0,750|652,0,750|0.119px|
| Ion_Defense |1631,0,675|816,0,675|0.489px|
| LTUR (normal/Marked) |953,0,784|477,0,784|0.118px|

Turret attachments, native art, scales, turning, weapon definitions, damage,
reload times, faction bindings and selection geometry unchanged. AA pass11 remains
pixel-identical to its approved source. Checkers now use manifest TileSize/TileScale
instead of hardcoded64/1448. Earlier art-pass reports retain historical offsets;
the current runtime settings above supersede their muzzle calibration.

`review_defense_muzzle_followup.py` resolves current engine actor/sequences and
composes every facing with a green marker at the projected muzzle. All three
32-facing sheets were inspected: markers sit on the barrel openings. Source hashes
are recorded in registration.json. These are diagnostic compositions, not live shots.

Normal build succeeded; all four engine checks and Sentry/AA/Ion/Nod Laser
regressions pass. Ion historical-baseline test also now explicitly allows the already
installed global construction sequence (unrelated to this fix). New live firing
review remains pending; restart the building test to reload rules/sequences.
