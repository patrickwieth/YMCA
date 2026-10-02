# Shared running gear and further ground combat bindings

## Scope and compatibility

This increment adds **14 base combat families**: the ten remaining Nod groups and four remaining
China groups in the generated ground inventory. It does **not** complete all factions or promoted
variants. Current native coverage: **45 faction bindings / 44 editorial families / 124 combinations**.
**44 whole combat groups remain**, plus variants; see [missing-vehicles.md](missing-vehicles.md).

Running gear now has a `compatibility_class`; chassis declare `allowed_running_gear_classes`.
A class is not an engine locomotor. MLRS, SSM and Prism use one **Light Tracks** component
(`light-tracks`, class `light-tracks`, locomotor `wheeled`). The separate **Light Tracks (Tracked
Profile)** component uses the same class but `lighttracked`: different terrain/crushing behavior
is deliberately not silently merged. Explicit native assembly choices remain an additional safety
constraint; a class match alone does not enable unreviewed assemblies or reinforced upgrades.

The three duplicate MLRS/SSM/Prism gear IDs migrate on schema-1/schema-2 profile load. Names,
design IDs and other parts remain unchanged; unknown parts still fail validation. No user library
files are rewritten until the user saves. Existing immutable maps are not rewritten.
There are now **14 rather than 16 gear components**, with no new vehicle-specific gear for the
14 added bindings. All **96 prior calculator combinations retain every numeric result** after
canonical gear-ID substitution (compared against the previous committed fixtures).

The new families also share **Integrated Mount** and **Integral Stores** hardware allocations.
These do not provide interchangeable ammunition or passenger loadout editing: each complete
stock weapon/ability package remains bound to its actor. Costs are shared, not per-design rebates.
Hull/module mass, power and price splits remain constructed, experimental allocations. Matching
original prices is not independent calibration evidence. The shared heavy-track turn capacity is
48, but existing hull limits remain unchanged; Mammoth does not gain faster turning.

## New bindings: exact raw stock baselines

| Actor | Vehicle | Cost / HP / speed / body turn | Locomotor |
|---|---|---|---|
| APC2 | Armored Personnel Carrier | 600 / 30000 / 135 / 48 | tracked |
| BIKE | Recon Bike | 500 / 11000 / 180 / 80 | wheeled |
| Beam_Cannon | Beam Cannon | 1250 / 24000 / 100 / 48 | wheeled |
| FTNK | Devil's Tongue | 700 / 40000 / 82 / 48 | tracked |
| HFTK | Heavy Flame Tank | 1000 / 75000 / 68 / 48 | heavytracked |
| HOWI | Howitzer | 550 / 15000 / 68 / 16 | lighttracked |
| SPEC | Specter | 1100 / 11000 / 100 / 48 | lighttracked |
| STNK | Stealth Tank | 1200 / 20000 / 135 / 48 | wheeled |
| TTRK | Chemical Truck | 1200 / 10000 / 92 / 48 | wheeled |
| WTNK | Microwave Tank | 1250 / 35000 / 100 / 48 | wheeled |
| charty | Inferno Cannon | 900 / 12000 / 90 / 48 | wheeled |
| chcrawl2 | Heavy Troop Crawler | 1000 / 45000 / 125 / 48 | tracked |
| chnukecann | Nuke Cannon | 2400 / 24000 / 55 / 8 | tracked |
| Bixi | Bixi Dragon | 900 / 16000 / 56 / 8 | wheeled |

Armor, locomotor, cost, HP, speed and turn are checked against the engine's **resolved stock
ActorInfo**, not merely local YAML fields. Every intrinsic stock armament must remain covered.
Complete projectile/warhead rules, offsets, delays, attack lists and conditional channels are
inherited rather than reconstructed with damage-only approximations.

- APC: cargo capacity, loading lock, drone attachment and occupied-driver capture restrictions.
- Beam Cannon: all six beam strengths, visual and charging channels, boost conditions/target types.
- Flame tanks: normal/force-fire channels, flame upgrades, death explosions and ambient sound.
- Specter: auto-deploy, deployment facing/terrain, normal/advanced cloak and damage uncloak.
- Stealth Tank: independent ground/AA missiles and both cloak modes.
- Chemical Truck: targeting dummy, triggered suicide, unload/disarm conditions and toxic explosion.
- Microwave Tank: disable effect, independent sound weapon and disabled-target exclusions.
- Inferno: normal/Black Napalm channels, fire effects, damaged-speed and movement modifiers.
- Heavy Troop Crawler: actual original initial infantry, six cargo places and firing ports. It has
  **no intrinsic gun**; no fake cannon or cloned infantry weapons are added to the carrier.
- Nuke Cannon: deployment, dummy aim, ammo/refill cycle and shootable spawned shell actor.
- Bixi: two spawned missile actors, reload graphics, aim pause and inherited artillery modifiers.

### Deliberate roster policy, not vanilla availability equivalence

These are custom Black Hand/Tank General rosters, not new vanilla national rosters. Nod admits
Legion Howitzer/Microwave/Chemical Truck and the otherwise Black-Hand-excluded Devil's Tongue.
China admits the Infantry General's Heavy Troop Crawler. This is the explicit national-admission
exception; vanilla actors and prerequisites are not edited. Other stock prerequisites, promotion
exclusions and replacement gates stay intact, including `tmpl`, `radar`, `infantry.any`,
`nuke_cannon.access`, `promotion.bixi_dragon`, `promotion.beam_cannon`, and relevant `!upg.*` gates.
The new `StockPrerequisites` binding prevents component engineering tiers from accidentally
adding stricter gameplay technology gates. Existing faction-wide modifiers remain inherited;
for example the displayed 700-credit Devil's Tongue baseline is not its Black Hand discounted cost.

## Verification and remaining risks

- **85 Python / 112 native tests**, 124 native numeric combinations checked independently.
- Saved single/roster gear migration, class rejection, unknown-ID rejection, isolated factions,
  all fourteen Nod/eight China designs frozen into maps, and complex stock-package assertions.
- `--check-custom-faction-previews`: **45** headless preview bindings; the fourteen new baselines
  and armament coverage additionally checked against engine-resolved stock rules.
- Preview handles legitimate extra voxel body parts (APC/Inferno) without treating them as duplicate
  variant bodies, and selects Specter's initial undeployed state. Emperor/SSM guards remain.
- Nod differential: **1685 / 1706**, 21 inherited additions, zero unexpected diagnostics.
- China differential: **1685 / 1705**, 20 inherited additions, zero unexpected diagnostics.
  Reports: `tools/modular/designer-{nod,china}-combat-validation.md`.
- China's inherited `CargoInfo.PassengerConditions` references missing `mort`, `shok.nod`, `tecn2`
  and `tecn3` are also present in the stock actor. Differential normalization now handles that
  exact qualified field path; a regression test ensures new missing passengers/weapons stay failures.

Full mod lint is still not clean. These checks are **not a live production/combat/transport test**.
The user confirmed the earlier rotating viewer visually; the newly added cargo, deployment,
cloaking, charging and missile actors still need live verification. Aircraft, naval, economy/support
systems, unrestricted assembly mixing and promoted variants remain separate work.
