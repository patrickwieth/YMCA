# MLRS AA: premature detonation above raised terrain

Later shared protection for other missiles is documented in `missile-height-policy.md`.
That follow-up adds a separate relative cruise-height option and an engine patch;
the baseline fixture now disables both height options to reproduce the old bug.

Local Game fix only. No release, deployment, current-game termination or engine
change. Existing work in MissileCA was preserved byte-for-byte after temporary
diagnostic logging was removed.

## Reproduced cause

`227mmAA` uses CruiseAltitude 2c0 but did not enable TerrainHeightAware. On a
four-level plateau (world Z=2896), the long-range dedicated AA missile could enter
its cruise phase and descend toward an absolute altitude of 2048 — below its own
launch plateau. Physical terrain collision correctly detonated it there. This is
not an insufficient weapon range or an arbitrary short projectile lifetime.

The native baseline dedicated shot detonated after 12 projectile ticks, approximately
8.3 cells short of its target. The diagnostic recorded a terrain collision at
Z=2530, 366 world units below the plateau surface. The same flat-ground shot worked.
Other launch/target geometries can work even without the fix; this was not a claim
that every AA missile always failed. The baseline actual-unit test hit a stationary
Orca, but hit the moving Orca less consistently.

## Narrow correction

`mods/ca/rules/gdi/weapons.yaml`: enable `TerrainHeightAware: true` on `227mmAA`.
The dedicated, hover and upgraded AA variants inherit it. Cruise steering and
lookahead now use the existing physical-terrain-height implementation rather than
steering below elevated ground. Ground artillery, shared missile code, weapon
range, fuel, speed, damage, reload and point defense remain unchanged. Terrain
collision has not been disabled or bypassed.

The existing nested `Warhead@1Dam` under the dedicated Projectile is not repaired
in this flight fix: doing so could independently change its damage balance.

## Native regression

`MlrsAaFlightProbe` is diagnostic-only and requires `YMCA_MLRS_AA_PROBE=1`.
`tools/prepare_mlrs_aa_probe.py` creates a disposable fixture from a fresh Mountain
Valleys 2P seed42 export (112x112). `--baseline` overrides the inherited flag back
to false in the fixture only, without changing runtime files. Wait for
`mlrs-aa-flight.json` with `test-handcrafted-map-runtime.ps1 -WaitForResultFile`.
The baseline intentionally returns Passed=false and the runner rejects it.

- Four AA weapon variants, on flat ground and a raised plateau: all eight fixed
  shots reached the target vicinity without premature terrain contact. Flat-ground
  traces are identical before/after.
- Actual MLRS.AA armament versus an actual Orca on the plateau: both stationary and
  controlled lateral movement cases take damage. The aircraft is moved explicitly
  to make tracking reproducible, not flown by AI. Stationary damage was unchanged;
  moving-target damage increased from 2700 to 10800 in the fixed test window as
  more missiles arrived, without increasing damage per hit.
- Isolated build and `tools/test_mlrs_aa_flight.py` pass. Evidence is archived in
  `art-source/mlrs-aa-flight/`. Temporary collision-reason logging is baseline
  evidence only, not active production code.

These checks establish this specific terrain-height defect and its correction;
they do not make all AA shots unavoidable or disable normal misses/interception.
