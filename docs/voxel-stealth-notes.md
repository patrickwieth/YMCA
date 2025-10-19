# Voxel Stealth Notes

## Cloak Trait Setup
- `^CloakStealthVoxel` lives in `mods/ca/rules/defaults.yaml`.
- Keeping `CloakStyle: Alpha` with `MinCloakedAlpha` / `MaxCloakedAlpha` set to `0` hides the voxel completely; switching to palette cloaking shows the orange “selection box” unless per-player palettes exist.
- Nod-style flash is already present (`EffectImage: stealthfade`, `CloakEffectSequence: reversed`, `PlayEffects: true`).

## Palette Experiments (If We Revisit)
- `PaletteFromPaletteWithAlpha@cloak_voxel` in `mods/ca/rules/palettes.yaml` clones `tsunit`; we can stack `TAStealthTankCloakPaletteEffect@Voxel` for shimmer, but every player needs a matching `cloak_voxel` palette.
- `PaletteFromPlayerPaletteWithAlpha` creates per-player variants; missing ones cause `Palette 'Multi0'` errors or orange boxes.

## Periodic Flash Concept
1. Add a sequence in `mods/ca/sequences/misc.yaml`:
    ```yaml
    stealthflash_voxel:
        Defaults:
            Filename: stealthfade.shp
            Scale: 0.45
            Length: *
            Tick: 30
            Offset: 0, 0, 30
            ZOffset: 3c128
            BlendMode: Additive
        idle:
    ```
2. Define a helper weapon in `mods/ca/weapons/other.yaml`:
    ```yaml
    StealthFlashEffect:
        ReloadDelay: 90
        Range: 1c0
        ValidTargets: Ground, Air, Water
        Projectile: InstantHit
        Warhead@Fx: CreateEffect
            Explosions: stealthflash_voxel
        SoundVolume: 0
    ```
3. Optionally add an alias explosion in `mods/ca/weapons/explosions.yaml`:
    ```yaml
    StealthFlashVoxel:
        Warhead@Fx: CreateEffect
            Explosions: stealthflash_voxel
            ValidTargets: Ground, Air, Water
    ```
4. Attach to the Outpost in `mods/ca/rules/china/vehicles.yaml`:
    ```yaml
    Cloak@NORMAL:
        ...
        PauseOnCondition: cloak-force-disabled || base-reveal || disabled || driver-dead
    PeriodicExplosion@StealthFlash:
        Weapon: StealthFlashEffect
        RequiresCondition: hidden
        ResetReloadWhenEnabled: true
        LocalOffset: 0, 0, 256
    ```

## Workflow
- After every change run `tools\\test-game.cmd` (close the OpenRA window manually once the map loads).
- Check `C:\Users\<user>\AppData\Roaming\OpenRA\Logs` for crashes (`exception-*.log`).
