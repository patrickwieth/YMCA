# Height gameplay and terrain soak follow-up

This is scoped evidence, not completion of the entire terrain acceptance checklist.
Bridges remain excluded. Evidence root: `%TEMP%/ymca-terrain-final`.

**Subsequent completion loop:** [terrain loop follow-up](rubberduck-terrain-loop-followup.md)
records the real shipyard/travel tests, flat-shore corner fix, legacy editor
invalidation, rendering optimizations and a completed 30-minute eight-bot match.
The older layout-only soak and open items below describe this earlier stage.

## Fixed: height-aware missile lookahead

`Projectiles/MissileCA.cs` previously multiplied map height by 512. Rubberduck's
isometric height increment is 724 world units, and ramps need interpolation.
Lookahead now samples the same physical surface as collision:
`position.Z - map.DistanceAboveTerrain(position).Length`. Its initial previous
height also comes from the current surface instead of an invented height zero.

The height-combat utility checks 125 points: five heights, flat/four ramp directions,
center and four off-center positions. This tests the lookahead sampler, not every
missile configuration's ability to clear a cliff. Weapon speeds, damage and guidance
settings are unchanged.

## Fixed: direction-dependent visibility of raised targets

The original reveal disk was measured after projection into the shroud plane. A
height-four target four cells away was visible in three directions but not the
fourth in the actual fog-enabled Challenger fixture (`combat-fog`, rejected).

`Traits/RubberduckRevealsShroud.cs` measures horizontal range before projecting the
eligible terrain cells. Reveal and generated-shroud sources use the same mapping.
Range modifiers, conditions, footprint/center/ground modes, sharing, height limits
and source removal remain handled by the existing Common traits.

Visible raised cells also reveal their projected vertical face down to the ground
footprint. Roof-only projection initially left black holes below visible cliffs;
column-coverage assertions and the final captures cover that correction.

Compatibility is intentional: the CA assembly precedes Common and supplies
`RevealsShroudInfo` / `CreatesShroudInfo` under the original YAML names. No hundreds
of YAML overrides or old map keys need renaming. Non-Rubberduck maps instantiate
the original implementations. Utility checks verify both Info types resolve to the
compatibility classes. The correction does **not** introduce mountain ray-cast
line-of-sight blocking; existing default `MaxHeightDelta=-1` remains unlimited.

## Actual weapon/turret/sight fixtures

```
--rubberduck-height-combat-test OUTPUT [fog=true] [unit=challenger_tank]
```

Six representative vehicles: Challenger, MTNK, LTNK, T-34, chbattle and SEEK.
Each has sixteen cases: four compass directions × flat ground, high edge firing
low, low firing at a high edge, and an intervening height-four blocked ridge.
Final successful directories:

- `accepted-challenger_tank`
- `accepted-mtnk`, `accepted-ltnk`, `accepted-t-34`
- `accepted-chbattle`, `accepted-seek`

These contain **96 actual combat checks and 96 actual owner-shroud checks**.
All six sessions also dispose their sixteen revealing actors and verify that all
sixteen targets return to fog. Final captures are taken after this lifecycle check;
terrain stays dimly visible while enemy vehicles disappear.
The source must fire without changing its cell. Real inherited weapons and turret
traits are used; auto-targeting is disabled only in the diagnostic actors.

Important fixture corrections, not gameplay changes:

- Neutral biological-driver targets decay independently of being shot. Targets
  now belong to Creeps; damage is counted via the shooter's applied-damage callback,
  not assumed from total HP loss.
- Positive up/down cases use an edge perch. A deeply inset roof itself obstructs
  a low-trajectory shell; that is not a valid positive control.
- China/Seeker splash can damage a target behind the ridge while the projectile
  correctly stops in rock. A map-only, damage-free tracing warhead records impacts
  on the **authored five-cell ridge**. It does not change projectile motion, splash
  or damage. The final barrier assertion requires impacts and rejects any outside
  that ridge. Comparing to fractional/moving actor centers was also rejected.
- Earlier `combat-chbattle`, `combat-seek`, and appended failed intermediate trace
  runs are not final PASS evidence. Utilities reset the dedicated log on export.

This is representative terrain combat evidence, not every weapon/upgrade, aircraft,
jammer lifecycle or competitive balance combination.

## Fixed: opaque RGBA fog

The prior fog derivative copied full alpha 255 from its reference PNG. Indexed
`ShroudPalette` uses alpha 128 for full fog, but RGBA sprites bypass that palette.
Consequently even fully explored terrain was rendered black. The fog derivative
now uses explicit 128 alpha; unexplored shroud remains opaque. This changes rendering,
not explored/visible cell state or enemy visibility. Installed provenance records
the conversion, and original atlases are unchanged.

Actual normal/shaded large-map captures show dim terrain instead of black. The six
source-removal sessions prove that the transparency change does not itself reveal
the hidden targets. The installed PNG is reproducible with
`--rubberduck-shroud-art OUTPUT fog`.

## Legacy shore regression

The formerly failing irregular shoreline fixture was regenerated and run in the
client. Infantry, Challenger and DD all reached their destinations and rejected
the forbidden domain (ticks 125, 93 and 397 respectively). The historical failure
is not reproducible on the current build. `ShoreMovementProbe` now reports start,
target tile and both path counts if it fails again. No movement rules were weakened.

## Naval audit

```
--rubberduck-naval-audit MAP_DIRECTORY OUTPUT
```

The utility reads exported terrain and actual naval/foot terrain speed tables,
without altering maps. It checks graph regressions (narrow neck, erosion, diagonal
leak, rotation, mixed-speed reversal), then reports nearest-shore paths at 1/3/5-cell
square clearance. Costs include terrain-speed ratios; they are **not actual ship
travel times** or dock footprint checks. Diagonals conservatively require both
orthogonal neighbors.

`naval-body-access.tsv` additionally reports land access to every water body. A
nearest-port disconnection must not be mistaken for inability to reach another
coast or the sea. Enclosed water basins can be intentional. The initial nine-map
4P42 report found complete 3-cell near-port connectivity in Archipelago and complete
5-cell connectivity in Migration. Component-aware results in `naval-final` show that every start can access the main
Continents water body (2965 cells), both main Two Sides bodies (641/643 cells), and
multiple Water Basins pools. In Continents, the nearer 115-cell pool explains the
initial nearest-port disconnection; it was not evidence of an inaccessible sea.
Archipelago's main body has equal access costs for all four starts, as does
Migration. These are nine 4P42 terrain audits, not actual ship travel-time or dock
placement tests; no general naval fairness sign-off is claimed.

## Soak instrumentation

```
--rubberduck-soak-test MAP OUTPUT [ticks=15000]
```

The isolated map preserves terrain and adds `TerrainSoakProbe` for 15000 actual
world ticks. It explores the local player's map, cycles nine sectors and three
zooms, and records actor count, managed/private/resident memory, GC collections,
and bounded per-frame cadence samples every 500 ticks. The screenshot is requested
only at completion. Camera/fog changes exist only in the diagnostic fixture.

Frame sampling uses `IRenderAboveWorld`, not `ITickRender`: the latter runs with
logic ticks and cannot measure rendered-frame cadence. The interrupted initial
`soak/initial-logic-cadence-not-frame-times.log` is explicitly rejected as frame
pacing or completed-soak evidence. Wall-clock frame cadence is not a GPU timer.

The earlier `soak-final`, `profile`, and `profile-visible` runs had black fog and
are **not accepted terrain-render evidence**, even when their tick counter finished.
The final probe explores the actual render player's map after player assignment,
asserts that each camera sector is explored, and records window focus and graphics
settings. This avoids silently skipping exploration during PostWorldLoaded.

Final **`accepted-soak`**: 192x192, 16-player layout, 15000 simulation ticks,
602.51 seconds, 30 sample windows, 72989 rendered frames, all with window focus.
Nine sectors and three zooms were cycled. Viewport 1024x768 at scale 1.5, VSync on,
no configured frame/game-FPS cap. Actual fogged terrain is visible in the capture.

- Window median frame times: 6.88–13.18 ms; window p99: 10.43–45.77 ms.
- Worst frame: 576.82 ms. Camera/cache warm-up hitches therefore remain a finding,
  not an unconditional smooth-frame guarantee.
- Managed memory: 436–592 MB; private memory peak 2.37 GB, final 2.28 GB.
  Memory does not grow monotonically in this sample; it is not a proof against
  leaks in hour-long combat matches.
- Actor count: 5275–5278, mostly terrain decorations, not thousands of combat units.
- `accepted-shaded`: separate 1000-tick/40.83-second shaded smoke test with visible
  terrain, not a second long soak.
- `accepted-editor`: ten actual editor PASS categories, including legacy stamp
  removal/restoration, exact Undo/Redo, protection checks and save/reload.

This is a terrain/camera soak, **not a multiplayer match or an army/bot combat soak**.
GPU timer certification, long combat matches, full old-map contact review, dock
footprints and real naval travel-time fairness remain open. No bridge work is
included. Release build, contour regression (including all 72 previous closed
exports) and whitespace checks pass. All 33 original sheet hashes are unchanged;
the installed fog PNG and provenance reproduce byte-for-byte. Temporary review
maps were removed from the mod's map directory.
