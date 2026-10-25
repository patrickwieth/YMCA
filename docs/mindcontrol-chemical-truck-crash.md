# Mastermind / Chemical Truck crash regression

## Reproduction

User replay: `ca-2026-10-14T192721Z.orarep` (local diagnostic input; do not commit).
Reported release: CA v0.96.20, engine f31038bf1f3ccecb20e55eaa281288d6ec430360.
Map: Ore!, UID c35dc77a9c18886ea3b26d1f337caab0e4104b8e.

Reproduced in an isolated checkout of v0.96.20 with the matching engine. Only diagnostic
exception interpolation and presentation-only replay fast-forward were added for reproduction.
At simulation tick **24932**, actor **mast/755** attacks **ttrk/1080** and throws in
`MindControllerCA.AddSlave`. TTRK is the Nod Chemical Truck and inherits from DTRK.
Neither has MindControllableCA, but Vehicle target type makes them eligible for EnslaveVehicle.
The original exception message lacks `$` interpolation, hiding the actor names.

## Fix

- Shared `^MindControlImmune` grants an unconditional, separate immunity target provider.
  DTRK, `^ShootableMissile` and `^VoxelShootableMissile` inherit it. TTRK and all derived
  projectile actors therefore inherit the protection. EnslaveInfantry/EnslaveVehicle already
  exclude this target type. Other target types and projectile interception remain unchanged.
- Attacking rejects actors without MindControllableCA before starting control progress or
  applying ReleaseOnNewTarget. Existing controlled units remain untouched.
- AddSlave independently rejects a missing trait rather than throwing. This protects completion
  paths and custom-map actors, before capacity, ownership, conditions or experience can change.
- Normal supported targets and their disabled/paused checks are retained. Trucks do not acquire
  new mind-control support or biological-driver mechanics.

## Validation

1. Original release replay reproduces the exception at tick 24932 with the named actor pair.
2. Apply only the C# protection while retaining original release YAML: the invalid attempt is
   rejected at tick 24932 and replay simulation reaches tick **24936**, the end of the last
   complete input frame, with no exception or desync. Diagnostic process exits normally.
   The recording ends with an incomplete network frame; no later match result can be tested.
3. Apply the YAML immunity too: old replay desynchronizes at network frame **8113**, because
   corrected target eligibility changes earlier simulation decisions. This is NOT a successful
   full-patch replay run; do not disable sync checks and claim replay compatibility.
4. Build master against its pinned engine 2fb9271bb7cd4b645ed24c11166b9f7a3e009931 succeeds.
5. Extended `--check-mindcontrol-targets` passes against **1557 concrete resolved actors** and
   all **4 weapons** used by MindControllerCA armaments: commandeer, enslaveinfantry,
   enslavevehicle and hack. No potentially eligible target lacks MindControllableCA.
   DTRK/TTRK and affected projectile actors have unconditional immunity without gaining a control
   trait; both enslave weapons exclude immune targets; MTNK retains mind-control eligibility.
   Conditional target providers are evaluated together over Boolean condition states. Unsupported
   numeric/stack-count predicates or more than 16 variables fail explicitly rather than receiving
   a false clean result. The audit conservatively ignores relationship/reachability constraints.
6. Audit controls verify ordinary tank eligibility, all six harvester variants' immunity (including
   the Scrin water target), and reproduce the grounded V3 vulnerability by omitting its immunity
   from the audit input. That mutation must yield the witness state `airborne=0`.

## Additional actor audit

Ten additional actors were eligible for EnslaveVehicle without the control trait when `airborne=0`:
`atomparabomb`, `teslaparabomb`, `bixi.missile`, `icbm`, `rice_shell`, `rice_passengershell`,
`v3`, `v3.kirov`, `v3b`, `v4`. Their two projectile base templates now inherit `^MindControlImmune`;
no per-projectile patch list is necessary. New descendants receive the protection automatically.

Harvesters initially look suspicious if conditional providers are checked independently, but they
already grant MindControlImmune whenever their Vehicle target is exposed. In particular, the Scrin
water target implies the ordinary target's enabling condition. They were not changed or counted as
additional bugs. The controller-side guard remains necessary for custom-map actors; this audit
covers default mod rules, not every third-party map's override.

Run the rules regression with the built utility and appropriate ENGINE_DIR/MOD_SEARCH_PATHS:

```
dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-mindcontrol-targets
```

Diagnostic replay instrumentation is confined to the separate reproduction checkout and is NOT
part of the production fix. Master and v0.96.20 use different engine revisions; do not build a
release from this checkout assuming it is already the v0.96.20 release line. No release or push
is performed as part of this investigation. A fresh-match gameplay check remains advisable.
