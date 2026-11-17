# Construction and native-cut follow-up (local Game/isometric)

## User requests implemented

- Non-Scrin buildings show the existing China construction stages during make and
  reverse make/selling. Other production/idle animation presentation stays static.
  Scrin buildings and defenses retain the previous static presentation. The resolved
  construction inventory already provides the correct small/medium/big stages.
- Both China power plants move another4 native pixels UP on their existing bibs:
  Bottom offsets0,12 and0,24 (previously0,16 and0,28). Selection/smoke registrations
  follow; bibs, actor centers, occupied cells, power and costs do not change.
- China War Factory moves another8px RIGHT: Bottom36,2 (previously28,2). Its selection
  follows; bib, footprint, exit cells and gameplay configuration stay unchanged.
- The user-approved Gatling platform loses ONLY its black/yellow perimeter strip.
  Existing wall pieces move inward; visible width128→120, native Scale1 retained.
  No rescaling, recoloring or replacement pixels. Bearing, turret and muzzle geometry
  remain unchanged. Runtime uses platform-cut/pass-04; pass03 remains archived.
- New Scrin4x4 bib: art-source/scrin bib/bib4x4.png,512x282 native pixels. Generated
  from translated pieces of the reviewed3x3 with binary seams. Existing bibs untouched.
  Prepared artwork only: no building assignment/runtime binding changed for4x4.

## Presentation implementation

RenderSprites.AnimatedBuildingSequences permits selected body sequences to bypass
only the frame-zero presentation override. The override callback may return null to
use the original playback sequence AND its current frame. This distinction matters:
checking only whether a callback existed would still freeze construction at frame0.
No animation clocks, make/sell callbacks or conditions are replaced. Scrin explicitly
uses an empty allowance, including its separately inherited defenses. Other mods
keep the empty default and their existing behavior.

## Checks / evidence

Isolated Release build and normal test-game.cmd --check-only succeed without stopping
any user process. Engine checks load the actual construction sprite reservations,
compare live versus static forward/reverse frame presentation and completion clocks,
verify that production remains static, and resolve the new building placements and
Gatling base. Pixel provenance tests trace every visible trimmed-platform pixel to
its exact old coordinate, verify the bearing is unchanged, and verify all4x4 colors
come directly from the original Scrin source. Grid, Gatling, faction-binding and Scrin
bib regression tests pass. Native composed reviews are not live screenshots.

Reviews:
- art-source/chinese gatling/platform-cut/pass-04/cut-review-4x.png
- art-source/chinese gatling/platform-cut/pass-04/all32-native.png
- art-source/scrin bib/four-by-four/before-after-2x.png

No original artwork/map overwritten; no commit, push, release or deployment.
Live visual/production review remains pending after restart.
