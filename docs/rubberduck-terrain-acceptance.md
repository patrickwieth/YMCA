# Rubberduck terrain acceptance — not complete

Scope: clean generated and editor terrain. The subsequently supplied bridge family
is covered separately in [six-part bridges](rubberduck-bridges.md). A passing movement
probe is not a visual approval, and topology changes need new gameplay validation.
Original assets, resources, protected landings and collision must not be disguised
or silently rewritten to improve a screenshot.

## Requested runtime/legacy closure follow-up

The [continuous terrain loop](rubberduck-terrain-loop-followup.md) is the current
record for the subsequently requested legacy, naval and performance work. It
includes actual dock production and reciprocal ship trips on all five water
presets, a corrected flat-coast movement bug, shared legacy-foot decoration with
editor invalidation, and a completed 30-minute eight-bot 192x192 match. It also
records rejected optimization attempts rather than counting them as successes.
Final regressions and their exact scope are recorded there: 144 actual naval legs,
96 combat and 96 sight checks, editor/history checks, plus 45000 ticks of eight-bot
combat in 1800.70 seconds. The four requested follow-up areas are closed in that
matrix, without claiming universal balance or unused asset-family coverage. Unused authoring
families below are not silently declared mapped or implemented by runtime tests.

## Original-family reference (not a production fix)

The [closed original reference](rubberduck-original-closed-reference.md) now shows
one complete rectangle and four closed L shapes in the client. Original straight,
inward and outward pieces cover all four orientations, including rear closures.
This establishes compatible source artwork, not repaired production topology or
height-four collision. The 87,-23 fallback junction and broad blocked aprons were open at that stage;
the later legacy-abutment treatment is described in the closure follow-up. An overfilled diamond in the earlier open-roof diagnostic was also corrected.

The subsequent [height-four integration test](rubberduck-closed-height-four.md)
uses actual clear roofs, blocked foot rings and shared runtime/editor clipping.
Roof/ground vehicle routes and real editor Undo/Redo, rollback and save/reload
pass in their scoped fixtures. The [contact/ramp follow-up](rubberduck-closed-contact-and-ramp.md)
adds same-family side reconstruction and a three-wide rear ramp with real movement
checks. Rejected pinning/short-family candidates are not active. The latest
[installed-family rollout](rubberduck-closed-family-rollout.md) now enables the
normal four-ramp editor stamp, compatible normal replanning, and a conservative
component selector in the export path. Front-mouth contacts were refined without
changing alpha/collision. The [generator planning follow-up](rubberduck-closed-generator-planning.md)
now reserves compatible feet before economy: the Mountain Valleys sample uses
436 closed actors on its four homes, with normal/shaded movement and production
editor evidence. The fit also passes eight rotation/reflection checks. The later
[required-plateau completion](rubberduck-closed-plateau-completion.md) removes those
plateau fallbacks: 72 exports cover 866 fully closed plateaus. Other terrain systems
and the other eight presets retain their established behavior. This is not completion of every mountain contour
or overall visual/performance acceptance.

The [blocked-mountain material update](rubberduck-mountain-materials.md) replaces
the small repeated wall sample with projected original rock planes. All nine
terrain binaries are unchanged; final normal/shaded close views and editor checks
are recorded there. This changes material, not the stair-step collision contour.

## Height gameplay, fog and measured soak follow-up

The [height-combat and soak follow-up](rubberduck-height-combat-and-soak.md) fixes
isometric missile lookahead, direction-dependent raised-target vision, projected
cliff-face shroud holes, and opaque RGBA fog. Six representative vehicles pass
96 actual combat and 96 sight checks plus six source-removal sessions. The old
irregular-shore movement failure no longer reproduces. An earlier visible-terrain
192x192 run completed 15000 ticks in 602.51 seconds and exposed 576.82 ms
camera/cache hitches. The later closure loop fixes cold clipping, palette/lighting
work and expensive range searches, then completes an actual 30-minute eight-bot
match; its measured limits and remaining isolated hitches are recorded separately. The earlier black-screen soak is explicitly rejected as rendering
evidence. Nine-map naval body/access reports do not replace actual travel-time
fairness or dock-footprint tests. The gates below are not silently closed by these
scoped results.

## Targeted fixes with evidence

- Flat coast endpoints and side outer joints: corrected sockets and footprint;
  see [coast evidence](rubberduck-flat-rock-coasts.md).
- Ordinary grass/sand/dirt/shore edges: shared visual blending, height/rock
  exclusions and editor invalidation; see [material evidence](rubberduck-material-transitions.md).
- Ramp matte fringes and shaded plateau roofs: corrected source sampling and
  tileset-specific roofs; see [ramp evidence](rubberduck-plateau-ramps.md).
- Rear rock rims and retaining-wall texture scale: source-traceable bounded
  profiles and consistent strata; see [face evidence](rubberduck-native-cliff-faces.md).
- Native roof layers: matched original grass transitions to the six straight
  variants and outer corner, plus frame 12 to inward cliff piece 16;
  shared runtime/editor assets, no recoloring of the earth cap. The outer corner
  also retains its original translucent contact shadow instead of keying it away. See
  [original layer pairing and before/after evidence](rubberduck-original-layering.md).
- Authored grass-foot variants: complete original straight/outer/end pieces now
  supply the foot details, with unchanged logical anchors and collision. See
  [source mapping and runtime comparisons](rubberduck-authored-cliff-feet.md).
  Later legacy abutments make selected exposed reserved feet visibly rocky;
  the underlying blocked soil and all collision stay intact.
- Native/vertical junction diagnosis: the exposed foot at 87,-23 has an unreserved
  diagonal endpoint; roof alignment alone cannot close the foot. Two map-local
  tapered trials were visually rejected and removed, not installed. See
  [geometry and evidence](rubberduck-cliff-junction-geometry.md). The later shared
  abutment treatment avoids the rejected warp and preserves saved-map topology.
- Convex-foot fringe gaps: the existing soil edge now follows diagonally reserved
  corners as well as straight feet; third-ring edits/Undo/Redo/rollback are covered.
  [Evidence and footprint census](rubberduck-cliff-foot-fringe.md#convex-foot-qualification-and-invalidation-follow-up).
  This repairs missing borders, not the broad brown cores or native/fallback geometry.
- Low cliff-foot soil fringe: softens the adjacent grass edge without repainting
  blocked cells; foreground-roof occlusion and editor invalidation checked.
  See [joint review and fringe evidence](rubberduck-cliff-foot-fringe.md).
- Foreground terrain occlusion: water no longer paints across the diagnostic
  plateau roof. Shared geometry clipping also retains visible parts of ground
  transitions and the existing soil fringe; see
  [occlusion evidence and remaining limitations](rubberduck-material-occlusion.md).
- Safe generated coast smoothing: symmetry/component/feature protection;
  see [smoothing evidence](rubberduck-coast-smoothing.md).

## Continuous review batch

The [nine-preset review](rubberduck-terrain-review-matrix.md) now covers fresh 4P42
normal and 8P43 shaded exports, 18 actual overviews and selected closeups. It found
and fixed missing raised-mountain rendering in Arabia, Battle Nexus and Hill
Fortress, plus ramp-less mountain corner safety. Their corrected maps have real
infantry/tank round trips. This is not just a texture change: only previously
blocked mountain cells were raised, but full sight/projectile behavior still needs
separate validation.

Ordinary Grass A/B, sand and Dirt A/B transitions are now tested. The material cache
has exhaustive cache bounds and a 192x192 editor stress measurement; this
is CPU/cache evidence, not full GPU/combat performance approval. The subsequent
[native spatial water integration](rubberduck-native-water.md) splits the cache
into 624 ground and 576 primary-water sprites, with live tests on all five water
presets, both tilesets and two shrouded runtime cases. Secondary water 1060 now
loads a further bounded layer only when used; mixed shores and actual boat movement
are checked in both tilesets.

## Later fog/network/seed verification

The [fog, network and seed follow-up](rubberduck-fog-network-and-seeds.md) records
80 visible shore samples, 80 source-removal samples and negative controls across
both tilesets; selected foreground-roof/cargo captures; two real loopback clients
with 30 matching sync/position snapshots over 3000 ticks; and 36 new seed/player
exports. Those exports found and fixed an Archipelago home-resource-on-water bug.
Twelve twice-generated economy regressions now protect complete deterministic
patches. This is not a WAN/long-multiplayer or additional-hardware certification.

## Remaining acceptance gates

- [x] Installed plateau/legacy joins: required closed production contours plus
  the shared reserved-foot abutment treatment in both front orientations. Existing
  blocked soil remains blocked; no green camouflage or automatic saved-map rewrite.
  This does not declare unused source families interchangeable.
- [x] Protected coastal and plateau raster steps: existing grid geometry retained;
  collision, resources and protected footprints are not rewritten by the rendering
  fixes. Actual boat corner checks now also cover flat shores.
- [x] Native 6x6 spatial water texture in generator/runtime/editor rendering,
  including blocked water feet and procedural shore fading.
- [x] Source-backed native ground shores: [rollout](rubberduck-authored-shores.md)
  enabled by default after 1880 composition/clip checks, normal/shaded editor
  history tests, ten production closeup captures and 80 actual naval legs.
  These are authored ground-edge pieces, not a claim of invented foam animation.
- [x] Five ordinary authored land bases (Grass A/B, sand, Dirt A/B).
- [x] Secondary marker water 1060: native spatial phases, mixed shore masks,
  lazy loading, editor invalidation and boat movement. The subsequent source-shore
  fixture exposed and fixed missing primary/secondary water-to-water blending;
  this fix is active independently of the authored shore rollout.
- [x] Legacy water 1070/1080: corrected malformed base frames, spatial water and
  shore blending; sixteen saved indices retained, editor and boat checks passed.
- [ ] Other authored shore/transition families.
- [x] Nine-preset overview matrix, 4P42 normal and 8P43 shaded, with replacement
  captures after the mountain integration fix.
- [ ] Complete closeup matrix of ramps, concave/convex joins, expansion rings and
  protected shores; overview captures alone cannot certify these.
- [ ] Shroud/occlusion and unit-overlap review from both sides of each transition.
- [x] Bounded material-cache stress and full 192x192 editor refresh measurement.
- [x] Large-map rendered-frame cadence, shroud and long-match memory measurement:
  30-minute 192x192 eight-bot observer run, with active harvesting/combat, completed.
  This is not a GPU timestamp measurement or a constant-60-FPS hardware guarantee.
- [x] Disconnected map-edge strips: corrected partial shroud/fog coverage without
  terrain edits or projection clipping; all four edges plus generated fog and
  raised-mountain runtime reviewed. See [shroud evidence](rubberduck-shroud-edges.md).
  Broader visibility/performance cases remain covered by the open gates above.

The supplied six-part bridge is now implemented for new Continents generation,
with finite movement, layer-order, destruction, naval and editor evidence in the
[bridge follow-up](rubberduck-bridges.md). It is a ground-level crossing, not a
ship underpass or an elevated plateau bridge. Actual dock clearance and
reciprocal naval travel have a completed finite 4P42/8P43 matrix; universal
competitive balance across every seed remains a separate claim. Until these entries are resolved,
no statement that all terrain now looks good is supported.
