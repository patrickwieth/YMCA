# Rear rims, menu and Strategic front: player feedback follow-up

## Rear cliff edges

Rubberduck already supplies the rear-facing cliff pieces. `NativeCliffBody`
previously clipped their narrow authored roof lip behind the same plateau roof.
The lip now survives that self-occlusion, while genuinely nearer roofs and roofs
at a different height still participate in normal clipping. The existing source
pixels, four-pixel lip limit, source registration and hidden-interior exclusions
are retained. No artificial back wall, new artwork, terrain or collision change.

Actual normal client comparison: `%TEMP%/ymca-feedback/rear-before.png` and
`rear-after.png`. The previously missing NW/NE outline is visible again. This is
a narrow authored rim, not an equally tall visible wall on every side. The client
camera zoom differs between those captures; they are not pixel-difference evidence.
`editor-shaded/ingame.png` exercises the shared renderer and editor history/save
fixture. Original source PNGs and installed derivatives are unchanged.

## Strategic Mountain Valleys

Home team assignment is determined before construction; construction retains its
established order so dense home footprints do not become order-dependent failures.
After homes and ramps, but **before expansion rims, extra mountains and economy**,
the planner reserves a continuous front between team regions:

- Routes use native CPos cardinal neighbors, not cardinal steps in staggered MPos.
- Physical spacing accounts for MPos columns being twice the row spacing.
- A deviation cost favors the team axis rather than an equally short staircase
  wandering toward the top map edge.
- Dynamic programming chooses the complete ordered objective chain, with spacing
  and both endpoints checked against every player's team/home. Greedy placement
  previously exhausted the last usable pad on uneven-team layouts.
- Closed mountains cannot occupy the reserved corridor. If it intersects a proposed
  open-valley rim, that entire rim is omitted before painting, rather than cut into
  fragments. An existing natural gate does not count as an intersection. Expansion
  sites remain; economy is placed outside the reservation. No completed plateau is
  flattened and no resources are deleted to rescue it.
- Interior expansion sites are placed first in this mode, avoiding starvation of
  tightly packed interior homes. Tactical and Operational construction order stays
  unchanged. The reservation and route clone independently.

Other Strategic presets retain their existing terrain generators; this new early
reservation is specifically the Mountain Valleys implementation.

The fresh skirmish lobby now picks an opponent slot on the other locked team.
Previously the first empty slot could be on the human's own team. Map locks are
reapplied after restoring a saved lobby's client values.

## Whole expansion rims: subsequent feedback

The rim is collected in full and accepted or rejected as one proposal. For four
players, Strategic seed `234373457`, two intersected rims (centers `69,49` and
`26,46`) are omitted entirely. Homes, ramps, other mountains and the reserved
front are not carved or relocated by this change.

Validation: Release build; 24 twice-generated Strategic cases; seven atomic rim
rejection controls (including first/last proposed cells and preservation of
existing terrain/resources), plus an intact natural-gate control. Tactical 4P42
`map.bin` remains identical to `%TEMP%/ymca-feedback/regression-tactical/`.
Actual client evidence in `%TEMP%/ymca-whole-rim-play/` passes four occupied-slot
assertions and twelve autonomous checkpoint visits, ending at tick 1978. The
fixture uses the same accelerated/disabled-victory conditions described below.
Generator/build logs: `%TEMP%/ymca-whole-rim-*.log`.

Playable dimensions have not changed: automatic four-player Mountain Valleys is
96 by 96 cells (9,216), both before and after the front/rim changes. Exported
`MapSize: 98,114` includes non-playable padding; `Bounds: 1,17,96,96` describes the
playable rectangle. Omitting rims increases open terrain, not the map dimensions.
This comparison is against the earlier generated preset, not an unspecified
hand-authored map.

## Actual checkpoint progression defect

`Checkpoint` reimplemented `INotifyAddedToWorld`, inadvertently bypassing the
base `ProximityCapturable` trigger creation. Units could reach a checkpoint without
advancing. Rally-point initialization now uses `INotifyCreated`, leaving the base
added/removed/ownership lifecycle intact; the custom tick retains position updates.
Only entering the unit's currently assigned hierarchy advances its target. Merely
passing a different checkpoint no longer skips an objective. Disabled Autobattler
traits no longer issue periodic movement, preserving direct control outside their
active mode.

## Evidence and reproducible commands

Run utilities from `Game/engine` with the usual mod search paths:

```
--strategic-front-test
--strategic-front-playtest OUTPUT players=4 seed=42 size=auto
--rubberduck-closed-contour-test
```

- `--strategic-front-test`: **24 cases generated twice**, players 2–12/16, seeds
  42/43; complete front reachability, correct home ends, economy/terrain/objective
  determinism, owner-slot correspondence and independent cloning.
- `%TEMP%/ymca-feedback/accepted-front`: actual four-player client session;
  four locked team/spawn assertions and **12 autonomous checkpoint visits**. Both
  allied units travel 0→1→2, then both opposing units travel 2→1→0. Completion at
  tick 1978. No movement orders are injected. The fixture accelerates the wave
  interval to 100 ticks, removes starting bases and disables victory only to test
  both directions sequentially. Production terrain/checkpoints remain unchanged.
- `%TEMP%/ymca-ui-probes/feedback-final`: actual menu callbacks/framebuffers and generated
  lobby, including chooser cancel and back navigation; default opponent team 2,
  spawn 3 versus human team 1, spawn 1.
- Release build, whitespace check and closed-contour regression pass (180 valid
  contours / 543 rejected corruptions). The shaded editor records seven PASS
  categories including Undo/Redo, rollback and save/reload. All 33 immutable
  source hashes remain unchanged; Tactical 4P42 terrain matches its earlier binary.
- Additional exports: Tactical/Operational Mountain Valleys 4P42, all four other
  supported Strategic presets at 4P42, and Strategic Mountain Valleys 4P101/202.

Review screenshots: [rear rim](images/feedback-rear-rim.png),
[preset menu](images/feedback-presets.png), and
[Strategic lobby](images/feedback-strategic-lobby.png).

Earlier failing front layouts, the equal-cost top-edge route, and the client run
without checkpoint progression are rejected evidence, not successful tests.
This does not establish all-seed balance, a full multiplayer match or complete
terrain visual acceptance. Generate a **new** Strategic map to use the new front;
existing saved map layouts are not rewritten.
