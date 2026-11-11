# Supplied GDI Guard Tower — native integration

Update: a separate **1x1 gdibib1x1** decorative overlay is now installed (128x64,
Scale1). No occupancy or gameplay changes. The source body is color-matched by the
shared GDI palette layer; its original PNG below remains untouched.

Original: `mods/ca/bits/gdi/buildings/guard tower.png`, **128x150 RGBA**.
SHA256: `550479cc38b06c5e64cc1e9e8fbc1346b3cb81019ac5f6904db729a6489e7d76`.
The PNG is read directly and was not edited, resized or palette-converted.
It is a supplied static image, not a new Blender/VXL model or rotating sheet.

Existing **GTWR** uses image `gtwr.gdi.png` at Scale 1, Center anchor, offset
`-1,-56`. Original icon and eight-facing muzzle animation remain inherited.
Idle/damaged/dead use the same supplied image because no separate damage states
were provided. The old SHP make animation and legacy bib are removed; the PNG
includes its own base.

Visual selection bounds: `1086,1086,0,0`, height 111. Ground registration `(65,131)`
in the source frame keeps the alpha inside the visual envelope. Mouse bounds,
hit shapes, occupied cells, cost, HP, prerequisites, power, weapon stats and
turn speed remain unchanged. The two inheritance keys were named without changing
their order/values, to avoid a duplicate-key failure when adding the art fragment.

The one explicit firing-attachment change is `Turreted.Offset: 0,0,2007`.
`Armament.LocalOffset: 256,0,256` stays intact. The total muzzle height is about
100 native pixels above the registered ground origin, matching the upper window
level instead of emitting shots near the old short tower's base. This changes
projectile start height; it is not a claim that the old origin is unchanged.

[Source-composite binding preview](binding-preview-3x.png) (not a game capture).

The existing test-map Actor75 at **141,-15** is already GTWR. No map actor or
source archive was added/modified. Run `Game/test-gebaeude.cmd` to see the update.

Release build, actual engine PNG loading, actor trait construction, three dedicated
regression tests, GDI-art/selection checks and PNG case audit pass. Live appearance
and muzzle effects still need review. No game was stopped/restarted, no commit,
push or deployment. The separate smaller Ion Defense previews are not installed.
