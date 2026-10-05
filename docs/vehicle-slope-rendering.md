# Ground vehicles on ramps

## Enabled: native terrain orientation

`^Vehicle.Mobile.TerrainOrientationAdjustmentMargin: 256` enables the engine's existing
ramp-orientation interpolation on ground vehicles. It is deliberately attached to the
actual vehicle abstract, not `^VoxelVehicle`: that rendering abstract is also inherited
by non-mobile effects such as `atomparabomb`.

Voxel bodies have `BodyOrientation.QuantizedFacings: 0`, so their rendered orientation
retains pitch and roll. Sprite bodies use quantized yaw; `BodyOrientation` discards pitch
and roll for them. No flat sprite image is rotated or skewed to simulate a 3-D vehicle.
Mobile's underlying terrain orientation is enabled for both; the rendering distinction
is tested after body-orientation quantization, not by asserting that Mobile stays flat.

The fetched engine already projects voxel shadows using `Map.TerrainOrientation` in
`ModelRenderable` / `ModelRenderer`. No replacement voxel-shadow renderer and no permanent
engine patch were necessary. This does not imply that shadows spanning multiple terrain
planes or cliff edges are fully handled.

## Optional sprite fallback, not globally enabled

`WithTerrainContactShadow` is an **approximate contact ellipse**, not a light-projected
vehicle silhouette. Its perimeter and center are sampled against actual terrain heights,
and triangles are rendered on that surface. It is suppressed for actors above the ground,
out-of-map footprints, or footprints crossing a large height discontinuity.

It is currently enabled only on the diagnostic supply trucks. Do not add it indiscriminately
to all sprites: baked-in shadows would remain and could produce double shadows. Accurate
sprite cast shadows still require suitable separate masks/assets. The prototype is not a
solution for baked-in shadows, does not alter their pixels, and is not used on voxel models.
The contact approximation has a uniform alpha rather than a soft physically based penumbra.

## Reproduction

After building CA, use the normal utility environment:

```
OpenRA.Utility ca --rubberduck-native-plateau-lab SOURCE OUTPUT vehicles=true
```

The diagnostic map contains `TRUK` sprite vehicles and concrete `Heavy_Tank` voxel vehicles.
They ascend and descend all four ramp orientations, then park on ramps for inspection.
`TerrainCalibrationView.ZoomScale` provides repeatable diagnostic zoom independent of the
initial viewport zoom. The rest of the native-plateau fixture remains map-local.

`%TEMP%/ymca-vehicle-slopes-final/ingame.png` is the reviewed close-up: the tank and truck
are parked on the ramp's visible side instead of behind a retaining wall.

The safe runtime log identified by `runtime-results-path.txt` passed:

- 16 ascent/descent legs, with all four levels and settled ground-height checks.
- 16 rendered-orientation checks: voxel tilt observed; sprite body stays unpitched/unrolled.
- 8 stationary ramp/contact checks (reported voxel pitch 63 angle units; sprite pitch/roll 0).
- 4 native-foot entry/pathfinding rejection checks.
- 4 clear-terrain steep-edge checks in both directions.

Release build and map export/reload passed. This is representative truck/tank evidence,
not certification of every vehicle, turret/barrel combination, transport, shaded tileset,
fog configuration or prolonged Operational match.

## Native plateau fixture context

The same command without `vehicles=true` tests infantry and tanks. It uses the shared
four-level, three-wide, four-ramp plateau topology. Complete native front faces/corners
are fitted vertically to that height, preserving their lateral spread; they are not the
unwarped connection-gallery pixels. Rear faces and ramp retaining-wall transitions still
use the existing renderer. A low blocked apron explicitly accommodates the spreading feet,
while all original plateau surfaces and the twelve ramp-approach cells are checked unchanged.
That isolated fixture is diagnostic-only. A subsequent [generator integration](rubberduck-native-cliff-production.md)
now reserves safe low footings and selects fitted native pieces on supported contours.

The first apron run correctly failed the old probe's requirement that *both* cliff cells
be clear ground. `BlockedCliffFoot` now selects a separate test for the new collision case;
the default clear-ground steep-edge test was retained, not weakened.
