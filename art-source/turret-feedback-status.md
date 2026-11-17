# Turret feedback — actual implementation and remaining work

Pass counts and export checks are not artistic approval. Preserve supplied artwork,
accepted forms and historical renders. No commit/push/release authorization.

## GDI Missile Tower — user-authorized ATWR integration

Latest bundle: `gdi missile tower/pass-12/`, directly bound to ATWR using the native
PNG base, true VXL/HVA turret and exact source-pixel palette. The user rejected leaving
a transparent cut where the right-side turbines were removed: pass12 restores that
wall/paving with native source cuts. Model/export remain unchanged from9. Packed
Blender scene and full mesh/voxel rotations are retained.
Twin three-round rounded magazines, protruding noses, spine/trunnions, hydraulic supports,
rear service hardware and inferred upper tracking casing are modeled. Only bounded native
source cuts are used for textures; every exported voxel color has original-pixel provenance.
Source unchanged. Native source/side/rear/end-on and exported-cell turntables reviewed;
engine VXL/HVA readers pass. This is not live engine appearance or user artistic approval.
Engine-loaded runtime files and muzzle registration are checked; live GPU/firing review
remains pending. No game/Blender stopped, no commit/push/release. See that folder's README
and `docs/modular/building-art-latest-followups.md` for the shared locked-DLL rebuild status.

## Soviet Chemical Tower — user-authorized static TTUR integration

`art-source/soviet chem tower/pass-03/chemical-tower.png`: clipped dome/studs and lower
wall/footing completed using only original-pixel cuts, installed on TTUR. Original
opaque pixels and stats/2x2 occupancy preserved. No visible rotating turret; aiming
controller/weapon retained. Full source/gameplay/sprite checks pass in the current
isolated runtime; live review pending. See the source folder README.

## Scrin Shard Launcher — current isometric work

Latest feedback: uniform/empty armor surfaces lack the source's pervasive veins and
organic mechanisms. Material study43 adds connected interior vascular fields, inset
channels, junctions and gills, including rear/cap charts and a continuous receiver
field instead of repeated tiny stamps. Reviewed all directions; geometry/alpha remain
identical to30, crystals retain38's treatment. See `pass-43/review/` under the Shard
folder. **Not installed**: game remains30. Technical color extraction/holdout provenance
is documented in its README; tests do not establish artistic approval.

Previous texture-only request: material study38 was reviewed, **not installed**. Source-like
slate shading, corrected bounded shin/axle details, finer connected vascular highlights
and circumferentially unwrapped source-colored crystal facets replace30's simpler
materials. All vertices/topology/attachments and raw alpha silhouettes match30.
Side/rear/end-on/full-turn comparisons are under `scrin shard launcher/pass-38/review/`.
Earlier chalky, smeared and fragmented-highlight studies are preserved but superseded.
Runtime remains30; no game process stopped, no commit/push/release. See that README
for exact scope, technical checks and explicitly inferred hidden surface decoration.


Latest visual feedback rejects installed22. New source-led studies23–30 now exist:
convex/narrower standing legs, recessed shin mechanisms, lower yaw collar between
knees, shorter upper mass, overlapping receiver shells, correctly joined claw tips,
source-bounded armor/crystal detail and coherent bevel/recess materials. Fixed battery
and25-degree bore retained. Saved-model sweep and full/split controls pass after
correcting intermediate collisions. See `scrin shard launcher/pass-30/review/` and
its README. User subsequently authorized installation: pass30 is now bound to SHAR.
Mount0,0,375; turret0,-59.423128; muzzle577,-35,1490; selection height77. Actual-grid
engine muzzle maximum0.139px; source/backup/pixel/occlusion/gameplay tests pass.
Pass22 PNGs/YAML are backed up under `integration-pass30/`. Gameplay and1×1 occupancy
remain unchanged. Live firing/GPU review awaits restart; the running game was not stopped.


Supplied `art-source/scrin shard launcher/shard_launcher-332414650-0.png` rebuilt
through pass22 after direct reference/side/rear/rotation reviews. The cannon is now
25 degrees, with a shorter/flared five-plate housing. The four-shard magazine is
entirely FIXED on a grounded pedestal; only the upper cannon/neck vessels rotate.
The rebuilt base has source shin armor, knee intakes, inter-leg louvers and roots.
SHAR remains1×1. Fixed battery clearance was checked with swept meshes at64 angles;
fixed-collection holdout preserves proper occlusion. Native muzzle error max0.187px.
Existing five-shot AA-only weapon and other gameplay values remain unchanged.
See `art-source/scrin shard launcher/README.md` for iteration findings, bindings,
source cuts, native previews and remaining live review. No live approval claimed.

## Scrin Plasma Cannon — integrated after Shard revision

`art-source/scrin plasma/photon_cannon-1201896784-0.png` reconstructed through pass05
and integrated on PTUR as a1×1 ground/surface turret, no AA. Separate rooted base,
curved rotating cervical assembly, domed reservoirs and caged plasma aperture.
Original two-shot weapon/flash and other gameplay retained;64 muzzle projections
max0.177px. See `art-source/scrin plasma/README.md`. Live review pending.

## Chinese Gatling — native1×1 integrated

`art-source/chinese gatling` reconstructed from its32 supplied directional references.
The armored plinth and Soviet1×1 bib stay fixed; the twin six-tube gun carriage rotates.
Native32 muzzle registration covers all eight original AA/ground spin-up stages.
Existing shared two-barrel phase preserves left/right alternation across stages and
target switches without changing weapon/ammo/power/mine settings. Directional flashes
explicitly skip the stock minigun's empty odd frames; real lifetime/pixel checks pass.
See `art-source/chinese gatling/README.md`. Live review pending; no barrel-spin
animation is claimed. The remaining supplied China buildings are now integrated below.

## Remaining supplied China / Scrin buildings — integrated

All currently supplied China PNGs now have direct runtime bindings: both power plants,
barracks, Command Center with conditional radar roof attachments (including Nukular),
supply depot, airfield, propaganda/internet centers and the newly arrived War Factory.
Subsequent Nuke Silo, Naval Yard and1×1 Speaker Tower PNGs are also installed, including
PDL promotion. Silo launch and tower repair/PDL origins follow the new artwork.
CHSYRD now has its own supplied image; Soviet SPEN and its original art are unchanged.
Scrin Repair Bay, Storm Column (including temporary summon) and Rift Generator are
also integrated. Original PNGs and gameplay footprints/stats/production/docking remain
unchanged; reactor smoke and Storm Column discharge points follow the new artwork.
Repair-active/damaged states and Rift's active pulse no longer fall back to old sprites.
See `art-source/new-faction-buildings/README.md`. Build, engine loading/registration,
source/gameplay regressions and disposable map checks pass. Live review remains pending.

## Supplied Scrin buildings and Headquarter

SCRT now uses the separately supplied RGBA tech center.png; Foundry is deliberately
unused. Seven other supplied indexed building PNGs remain directly bound. MANI and
RIFT now use the new Field Manipulator and Rift Generator images; the earlier missing
Rift-file blocker is resolved. See new-faction-buildings/README.md.
Walls/fences (including laser fences) are no longer buildable by any faction; existing
map objects retain their other traits. Engine and full regression checks pass.
Their initially incorrect legacy palettes are fixed: individual embedded PNG palettes
on the bodies/death art, validated including alpha in world and editor. Supplied
RGBA Headquarter is now on SFAC and SFAC.Nukular, with short matching vein-end tips;
original backed up, architecture unchanged. See `art-source/scrin-building-bindings/`
and `art-source/scrin-headquarter/`. Live confirmation remains pending.

## Latest defense/alignment follow-ups — implemented locally

- Nod SAM: user approved60% size; now raised12px on the1×1 bib. Mount, muzzle and
  selection move together. New composed layout reviewed, actual-grid check passes.
- Soviet sprite flak: corrected the64-vs128px horizontal projection error; flash
  origin now lies at the main bore in all32 views. No art/weapon-stat changes.
- Sentry: shared China ground Gatling0G–3G and its spin-up/cooldown, **no AA**. Keeps
  Sentry art/flash; muzzle projection fixed for the actual grid. See
  `docs/modular/sentry-ground-gatling.md`. FTUR and China's weapon definitions intact.
- Nod Gun Turret: already gated by~disabled; verified unbuildable and still present
  at Actor15 on the test map. Laser Turret buildability unchanged.
- Nod Conyard: idle/damaged offset−22,−10 aligns its source worksite on Nod3×3 bib.
  Protected conyard.png not modified. See `art-source/nod-conyard-alignment/`.

Fresh checker compilation/tests ran in isolation to avoid the running game's DLL
lock; the game was not stopped. Runtime YAML/art bindings are in Game/modular and
load on restart. Live confirmation remains pending. No commit/push/release.

## Building test: China and Scrin added above existing factions

Normal `test-gebaeude.cmd` now adds China upper-left and Scrin upper-right, including
all standalone player buildings/defenses and promotion variants (not disabled
resource silos or temporary/helper actors). Existing display positions/source map
unchanged. Chinese Naval Yard gets water. Actual engine map/sprite/terrain/bounds
and non-overlap checks run before launch; regressions passed. Restart the test to
load this. See `docs/modular/china-scrin-building-test.md` for inventory/exclusions
and the two map-only Scrin wall compatibility fixes. No live session was stopped.

## Nod SAM centering and selection correction

User reported visible turret/socket misalignment and skewed selection. Root cause:
old calibration assumed64px horizontally, but MapGrid is128×64. Nod SAM now uses
attachment0,−147,1493, no classic perspective fudge and recalibrated horizontal
muzzle coordinates. Fixed PNGs/ammo states unchanged. Mouse/decoration diamond is
fitted to the base (140×72px, vertical center−25px, height100), not the old oversized
256×64 envelope. Actual engine-grid check and regression tests pass; composed
before/after/all32 overlays visually reviewed. Live confirmation still pending.
See `nod sam site/integration/README.md`. Other turret projection checks require a
separate audit; do not trust a hardcoded64px horizontal projection as live proof.

## Soviet AA faction/display follow-up

Russia/Iraq now receive SAM; North Korea receives buildable sprite flak (radar,
Defense queue). AI AA lists/build weights include flak. Both disposable test maps
show flak at148,-56 plus SAM at154,-62 and five practice aircraft. Normal building
test launcher picks this up. Source testmap1 remains unchanged. Rules/map regression
checks passed; running matches were not restarted.

## Soviet SAM pass12 — installed locally after requested iterations

User requested repeated reference-driven modeling/material revisions through pass12,
then isometric sprite export **and game integration**. Passes07–12 are now rendered
and reviewed; pass12 includes the complete32-direction rotation. Existing SAM uses
separate native base/turret PNGs with registered launch point and selection height.
Nike gameplay values, Nod SAM and both derived flak actors remain unchanged.
See `art-source/soviet sam site/README.md` for Blender/material/render workflow,
per-pass findings, preview links and engine checks. Live match visual review remains
pending; no running game was stopped. Earlier entries saying global SAM is unchanged
refer to the previous flak work, before this separately authorized SAM integration.

## Nod factory correction — installed locally

AIRS restored to original astrip.shp and disabled for new construction. WEAP.NOD
is the former shared/Legion factory's ground-production derivative for all four
Nod factions, using the supplied Nod warfactory.png previously shown on AIRS.
GDI WEAP.TD actor/art unchanged. Faction build gates, AI lists and only the Nod
factory display slot in both disposable test generators updated. Runtime build,
engine sprite/dependency checks and regressions pass; live drive-out review pending.
See `art-source/nod-war-factory/README.md`.

## Nod SAM approved art and ammo variants — installed locally

User supplied `art-source/nod sam site/sam_site-1179307279-0.png` and requested a
fixed extracted base plus an independently rotating3D launcher, minimum ten passes.
Twelve passes now exist. User approved pass12, four individually modeled
missiles/rails, armored open yokes, hydraulics and rear mechanics;32 native views.
Full-rotation review led to a depth reconstruction that centers the rack over its
bearing while preserving its source-angle projection. Fixed base retains attached
radar/service cabinet; known visible pixels are preserved and inferred hidden
roof pixels are separately masked. Scenery excluded, original unchanged.
See `art-source/nod sam site/README.md`, `preview-2x.png` and `rotation.gif`.
Five ammo states (0–4 missiles) now exist under `ammo-variants/`, each with32
facings. Only whole-missile visibility changes; the approved four-missile sheet is
byte-identical, all model geometry and fixed base preserved. No surviving rocket
moves between slots. Export/control-render tests pass. Now installed on NSAM:
fixed body, five ammo-conditioned sprite turrets, four-round magazine with full
reload after30 idle ticks, Nike.Nod Burst4/BurstDelays10/ReloadDelay30. Old popup
render/attack removed;128 muzzle projections agree within0.195px. Runtime build
and sprite/condition tests pass; live firing review pending. Stock Soviet SAM is
unchanged. See `art-source/nod sam site/integration/README.md`.

## Latest Soviet flak decision — supersedes the voxel status below

User rejected the live voxel presentation and requested rendered sprites.
Pass14 now provides a fixed native192 pedestal and32 rendered turret facings.
FLAK.SOVIET.SPRITE removes all voxel rendering/recoil traits; tests now select it
instead of FLAK.SOVIET.VOXEL. China construction restored; no separate barrel recoil.
Engine sprite/sequence/dependency checks pass; muzzle error0.189px. Original map,
art and stock SAM preserved. Normal building launcher and dedicated map both use
the replacement; dedicated title is now **Flak Sprite Test (local art)** (legacy
package filename retained). Live sprite review remains pending. Voxel work archived.
User subsequently requested the supplied pedestal and a1×1 bib: runtime now uses
`flak base.png` directly atScale0.4, with `sovietbib1x1`; turret attachment lowered
to131 to match its bearing. Full source-alpha diamond extent61px fits64px limit.
Native turret sprites unchanged. Engine and pixel/source regressions pass.
See `art-source/soviet flak/integration-sprite14/README.md`.

## EMP follow-up and cluster flak implemented

User confirmed EMP centering and guided flight, then requested a3×3 bib with free
front edge strips and preferred the old rocket's authored rotations. PATR keeps
its2×2 obstruction and fixed loaded/empty supplied PNGs. The3×3 decorative bib is
offset0,32, leaving both front strips unreserved; projectile is now the original
thawk32-direction sprite. Yaw256/pitch72, six initial flight ticks, guided flight
and ammo refill on support-power readiness are unchanged. The cutout-rotation
missile is only an archived study, not a voxel. New visuals need live review.
See `art-source/gdi-emp-launcher/README.md`.

Soviet flak now has a calibrated five-frame cannon muzzle flash and its own AA
weapon:32-tick reload versus Allied8, four4000-damage explosions and zero parent
damage. The four child impacts preserve actual detonation altitude and modifiers;
inner splash is widened to retain nominal Allied DPS at the central aim point.
Both test-map generators select the matching ground-test variant instead of the
old FLAK-23-AG. Runtime build, engine dispatch/sequence checks and Python tests pass.
Live new-flash/cluster firing review remains pending. Approved building art and
stock SAM are unchanged. See `art-source/soviet flak/integration-cluster/README.md`.

## Latest approvals implemented

Soviet flak pass15 is now installed on the sprite test actor: modeled base and
32-facing turret copied byte-identically,1×1 Soviet bib retained, attachment243.
Engine muzzle check0.189px; source/gameplay/map regressions pass. Stock SAM stays
unchanged; no separate barrel recoil.

Nod laser now uses50% of original size (supersedes65%). Attachment79, both muzzle
offsets953,0,784, selection724×724/height59. Original pixels/gameplay unchanged.
Engine muzzle check0.144px and resolved runtime scale/gameplay regressions pass.

## Matching rendered flak pedestal requested (subsequently approved/installed)

Fresh pass15 renders the existing modeled pedestal using the same camera/materials
as the turret, reviewed with the1×1 bib at32 facings. No new geometry redesign;
turret pixels exactly match14. Preview `pass-15/review/matching-base-2x.png`.
Not installed: current original-pedestal binding stays unchanged pending approval.

## Supplied Soviet kennel

`kennel.png` is now bound directly to KENN at native128×128, Bottom offset-1,17,
with a1×1 Soviet bib. Dog production/exits, footprint and gameplay unchanged.
Engine image/sequence/dependency and gameplay tests pass; live exits pending.
See `art-source/soviet-kennel/README.md`.

## Previous ordered work (Soviet voxel presentation superseded)

1. **Soviet flak:** pass12 exposes/lengthens the two parallel auxiliary tubes, with
   their clamps and sliding rods;13 adds small bores and red terminal bands.
   Real VXL/HVA export now exists in `voxel-01`:
   independent base, turret and barrel, each with one static HVA pose. Runtime
   `WithVoxelBarrelAxis` applies inclined recoil dynamically, not PNG recoil frames.
   `FLAK.SOVIET.VOXEL` is installed as an isolated editor/test actor, NOT a replacement
   for SAM or a new production-menu item. Native engine loaders/trait dependencies
   pass; recoil versus voxel-camera projection error <=0.056px. Current runtime DLL
   built successfully. Exported-cell previews reviewed, but live GPU/firing appearance
   has NOT been observed. Disposable map: `soviet-flak-voxel-test.oramap`, titled
   **Flak Voxel Recoil Test (local art)**; Ctrl+click ground uses an AG weapon override
   only in that map. Original testmap1 and SAM resolved rules remain unchanged.
2. **Allied flak:** user approved pass15 for installation. It is now installed on
   AGUN: byte-identical approved base/turret PNGs, fixed source pedestal and separate
   rotating turret, no old bib. Native loader and muzzle checks pass (<=0.172px).
   Two existing muzzle slots physically re-registered; weapon/rate/cost/power/HP/
   turning preserved. CRAM inherits AGUN, so redundant legacy-trait removals were
   removed from CRAM without changing its approved pixels or effective gameplay.
   Live firing/appearance still unobserved. Source base retains original visible
   pixels; the original turret's occluded top was reconstructed from adjacent metal.
3. **Sonic:** after Allied installation, pass19 adds wider coating tonal ranges,
   restrained warm worn-paint variation, bounded source paint patches and authored
   chips/streaks. Pass20 adds mottled paint islands and one-pixel inner highlights
   on lit-facing contours, as subsequently requested; pedestal pixels and silhouette
   remain unchanged through20. Pass22 now adds the requested paired rear/side pipes,
   cables on turret and pedestal, a concave little roof radar, and rear-dark/front-light
   contour selection. All retained main meshes match20; only the three old little
   radar placeholders were replaced. New hardware rendered/reviewed in32 facings.
   User then approved installation: pass22 is now bound to STWR with independent
   base/cables and rotating turret. Approved PNGs unchanged, physical muzzle
   registered (0.184px engine projection error), construction/damaged states loaded.
   Existing prerequisites including ~disabled preserved; test maps already contain
   STWR. Subsequently reduced to65% at user request, including pivot, muzzle and
   selection decoration (0.132px max projection error). Live review remains pending.
4. **Soviet helipad:** supplied native `bits/soviet/buildings/helipad.png` now directly
   bound for idle/damaged/active states. Scale1, Bottom offset -1,2; selection follows
   source ground contacts. Existing2x3 footprint,3x3 bib, docking/spawn/production/
   repairs/stats preserved; GDI HPAD untouched. Resolved engine bindings/gameplay
   regressions pass. Live landing test remains unobserved.

## Soviet flak visibility in the normal building test

The standard `test-gebaeude.cmd` previously generated its disposable map from
`testmap1.oramap` with the SAM display slot unchanged. The voxel flak was only on
its separately named test map. The standard preparation script now replaces exact
Actor37 at148,-56 with FLAK.SOVIET.VOXEL and adds the ground-fire recoil override
only in that disposable map. Original map bytes and global SAM remain unchanged.
Map preparation and engine-resolved map rules validated; no GPU visibility claim.

## Latest construction request

Old building make animations are bypassed by the generic China construction sequence,
selected small/medium/big by footprint area. Guard Tower/Ion use it too; sprite-less
voxel test actor remains exempt. Queue production times and gameplay footprints stay
unchanged. Actual engine sprite loading/dependency checks pass; live build/sell review
pending. See `docs/modular/building-construction/README.md`.

## Other feedback

- **Nod laser now pass10.** After the user called out the stalled material work,
  actual new steel/red finish fields, bounded source-coating variation and restrained
  surface wear were implemented and rendered. All121 meshes/transforms exactly
  match09, including the circular muzzle/recessed rectangular aperture. The user
  then approved installation: pass10 is now bound to LTUR with fixed base and
  independent turret. Native asset/dependency/muzzle checks pass (0.249px maximum).
  User then found the model too large: runtime scale reduced to65%, attachment,
  both muzzles and selection decoration resized accordingly (0.168px projection
  error). Original PNGs and gameplay footprint/stats preserved; live review pending.
- **GDI AA pass11 remains approved and installed on CRAM.** Its assets and muzzle
  registration rechecked unchanged. No silent geometry/material modifications.
- **Sonic history:**15 added the two nested front dishes;16-18 improved gold coating
  and corrected pale blotches. Current form is positively evaluated and must stay.
  The earlier base vent notch was corrected with physical louvers; further source
  base/mechanical details are still not declared complete.
- **Soviet Naval Yard water underneath** remains pending; do not confuse that request
  with the now-integrated new Soviet helipad.

Original assets, protected Nod conyard and `testmap1.oramap` remain untouched.
No running game/Blender session was stopped. No publishing or deployment occurred.
