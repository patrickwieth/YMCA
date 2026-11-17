# Nod SAM pass12 — installed locally

## Latest placement: approved60%, raised12px on the bib

User positively evaluated the60% size, then requested a higher position. The entire
assembly now sits12 screen pixels higher: body/placement offset−5,−32, turret
attachment0,−88,611. Mouse/selection offset follows (bounds950,977,0,−55; height60).
Muzzle local offsets, art pixels, scale,1×1 occupancy and bib are unchanged. The
front edge is intentionally no longer flush: it is12px behind the bib front.
Reviewed composed preview: `size-60-raised/before-after.png`. Actual-grid engine
muzzle check passes0.177px maximum error; rule/pixel/isolation regressions pass.
Fresh checker compiled/tested in isolation while the user's running game was left
alone. Restart the test to load this. Earlier front-flush settings are historical.

## Previous size change: 60% of original, still1×1 and front-flush

User requested reducing75% to **60% of original**. All body/placement/damage and
five ammo-state turret sequences now use Scale0.6. Original PNGs are unchanged.
The1×1 bib/placement/hitbox and weapon/ammo gameplay stay intact.

Base/placement offset−5,−12 keeps the visible front at31.8px, matching the bib's
32px front. Attachment0,−88,339 keeps the turret on the bearing; muzzle offsets are
112,60,913 /375,−60,913 /188,−94,913 /298,94,913. Selection/mouse bounds are
950,977,0,217 with height60. Layout previews in `size-60-1x1/` were visually reviewed.

Resolved-rule/pixel/faction-isolation regressions pass. Compilation succeeded, but
copying the updated checker DLL into engine/bin was blocked by the running OpenRA
process. That process was **not stopped**. The freshly compiled DLL was tested in
an isolated temporary binary directory: actual128×64 grid /all128 launch projections
pass, max error0.179px. Temporary binaries removed afterward. Normal launcher build
can refresh engine/bin after the user closes the game. Live60% review still pending.
Earlier75% measurements below are historical.

## Latest alignment: body flush with front of bib

User requested the building lower on the1×1 bib. Body/placement offsets are now
−5,−21 (previously−5,−53), moving the entire75%-scale assembly down24 screen pixels.
Turret attachment0,−110,577 produces the same shift; local missile offsets remain
unchanged so all four launch positions follow the model. Mouse/decoration bounds
are1188,1222,0,118 with height75. The base's visible front edge now meets the bib's
front edge within1.5px. No pixel repaint/rescale, footprint or weapon change.

`bib-front-alignment/` contains reviewed composed layout images, not live captures.
Build/regressions and actual-grid muzzle check pass (0.139px maximum). Restart the
test match to load the new placement. Earlier offsets below are historical.

## Latest: 75% scale and one-cell placement

User requested the same reduction as Soviet SAM: **75% of previous size**, Nod1×1
bib and exactly one occupied placement cell. NSAM now has footprint `x`, dimensions
1,1, ^1x1Shape and a ±512 hit rectangle. Base, placement, damaged states and all five
ammo-state turret sequences use Scale0.75. PNGs/model are unchanged.

The corrected registration is scaled consistently: attachment0,−110,1120;
barrel offsets140,75,1142 /469,−75,1142 /236,−117,1142 /373,117,1142.
Mouse/selection bounds1188,1222,0,−425 and height75 follow the smaller base. The
actual128×64-grid muzzle check passes for all128 muzzle/facing combinations, maximum
error0.139px. Nod SAM and Soviet isolation regressions/build pass. The rendered
layout preview in `size-75-1x1/before-after.png` was inspected (left is the original
misregistered large version, right current75%/1×1); not a live screenshot.

Weapon values, four-shot/ammo timing and faction availability remain unchanged.
Test maps already use NSAM and pick up the new size/placement on restart. Earlier
registration/2×2-bib measurements below are historical and superseded by this note.

## Registration correction after live user feedback

The previous utility incorrectly projected both axes with64px. Actual MapGrid is
**128×64**, so the old lateral attachment was doubled in-game: −28px instead of
approximately−14px. The old numeric muzzle check therefore could pass despite bad
runtime placement. The corrected utility reads TileSize/TileScale from the engine.

- Fixed bearing center refined to source pixel120,115 (ellipse center); the rotating
  render origin remains119,115. Existing PNGs and all five ammo sheets are untouched.
- Attachment `0,-147,1493` projects to approximately−13,−66px relative to actor center,
  matching the fixed bearing after base offset−5,−53. Turret sequence offset9,13
  cancels the render-frame pivot offset as before.
- Disable classic perspective fudge: the128×64 grid already supplies the2:1
  projection. Halve both horizontal muzzle coordinates, retain height. This fixes
  all directions, not just the initial view. Actual-grid maximum error0.180px.
- Mouse and decoration bounds are now1584,1629,0,−566 with height100: approximately
  140×72px diamond, centered25px above actor ground, fitted to the fixed base rather
  than the previous oversized/skewed256×64px envelope. No occupancy/hitbox change.
- Build, Nod SAM and Soviet SAM isolation tests pass. Ammo timing, weapon values,
  faction gates, fixed art and bib remain unchanged.

`tools/art/review_nod_sam_registration.py` generates actual-grid before/after and
all32-direction selection overlays in `registration-fix/`. These were visually
inspected; they are composed layout previews, **not live screenshots**. Live match
confirmation remains pending. Other turret utilities using hardcoded64px horizontal
projection need a separate audit; this correction changes Nod SAM only.

## Follow-up: Nod bib

At user request NSAM now displays the existing `nodbib2x2` decorative foundation
(`bits/nod/buildings/bib2x2.png`, depth offset−4096), hidden during construction.
The bib is actor-bound rather than faction-gated, so it also appears on the mixed
building-test map and after capture. Art scale, base/turret offsets, ammo behavior,
footprint, hit shape and weapon stats are unchanged. Restart the test match to load
this rule change. Resolved bib-path and existing SAM regression tests pass.


NSAM now renders the approved fixed base and all five independently rotating ammo
states. Runtime PNGs under `mods/ca/bits/nod/buildings/sam-*` copy approved exports
without resampling/repainting. `nsam.native` inherits legacy icons, muzzle effect
and the existing small construction animation; all live body/turret/placement
states select the new PNGs. Popup attack/embedded sprite rendering are removed.

- Native256, Scale1,32 directions, source facing start4.
- Fixed base offset−5,−53; turret sequence offset9,13.
- Bearing120,115 relative to ground133,181: attachment0,−147,1493.
- Classic perspective fudge disabled; actual128×64 grid projection checked against
  all four missile tips over all32 facings. Maximum projection error0.180px.
- Fire order matches exported removal order2,3,0,1. Engine uses Burst%4, so the
  LocalOffset array stores slots2,1,0,3 (not sequential firing order).
- Four-round AmmoPool grants stacked `sam-ammo` conditions, actual shot events
  remove one missile. Exactly one turret state is shown, none while constructing.
  Editor/actor preview shows the fully loaded state, not five overlapping turrets.
- Nike.Nod remains Burst4 / BurstDelays10 / ReloadDelay30. Whole magazine refills
  after30 ticks since the last actual shot via stock ReloadAmmoPool/ResetOnFire.
  This also refills an interrupted partial salvo after idle; the stock Armament
  restarts its burst after the same30-tick idle interval. The zero state persists
  during the full-salvo reload; no per-missile refill during continuous firing.
- Health, price, power, prerequisites, existing2×1 footprint/hit shape, range,
  damage and detection preserved. Stock Soviet SAM/Nike are untouched. The new
  attack no longer waits for the old underground popup animation by design.

The existing NSAM display actor on both building-test maps picks this up directly;
no replacement actor or source-map edit is necessary. Restart the building test
to load the new rules. No running game/process was stopped.

Validation: runtime build, `--check-nod-sam-art` (actual sprite loads, condition
selection/construction/preview and128 muzzle projections), source-byte identity,
resolved gameplay/stock-Soviet comparisons and existing art regressions. Technical
checks do not constitute live firing/target-switch visual verification, still pending.
No commit, push or deployment. Subsequent Soviet main/child-explosion timing and
four air practice targets are implemented separately; see
`docs/modular/air-defense-test-targets.md`. Both still require live review.
