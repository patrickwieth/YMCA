# Roof attachment candidate inspection

Reference: integrated rules at Game `9d8a9dec`; engine `484afea27c`.
No gameplay YAML or engine traits changed by this inspection.

## Flame turret: separate artwork exists

- Current Dragon Tank uses voxels `dragontank` / `dragontanktur` from
  `mods/ca/sequences/voxels.yaml`, with actor RenderVoxels scale 5.
- Overlord uses its own voxel image at scale 0.8. `WithVoxelTurret` resolves a
  sequence under the actor's RenderVoxels image and has no independent Scale
  property. Simply copying the Dragon voxel sequence is therefore not enough to
  assume correct size. It would also need a sequence entry under the Overlord
  image. No new scale trait has been added.
- There is ALSO a separate sprite `bits/generals/chdragontur.shp`. Existing
  `chflameboat` and `chasub` sequences use it as a 32-facing turret with
  `UseClassicFacings: False` (their Defaults include Scale 2).
- Overlord's existing Gatling roof uses a sprite `chgatlingt.shp` with 32 facings
  on its voxel hull. This is a useful structural precedent for a flame roof.

Both SHPs were exported through the engine Utility --png command and inspected
in `roof-sprite-preview.png`. The flame sprite is a compact separated gun, not a
complete Dragon hull. The image uses a test temperattd palette, crops transparent
padding and doubles native pixel size. It is NOT a rendered Overlord composite
and cannot verify attachment offsets, occlusion, palette/remap, recoil, muzzle
placement or in-game directional alignment.

Three new `Overlord Flame Roof ... / draft` designs reuse the existing flame
carrier/weapon/fuel costs and mass without retuning. They have no legacy target.
The proposed empty price is 2250 (no CP); PDL/Reflector use the existing global
CP pricing. New combinations require playtesting; a numerical quote is not an
assertion that this weapon pairing is balanced.

`experimental_carriers` is separate from the verified catalog allowlist. Only an
explicit `experimental_graphics: true` design can use this candidate in the
OFFLINE calculator. This is not an engine enablement or a future permission to
publish an unverified design; the future compiler needs its own mount validation.
The draft flag does not bypass payload compatibility, faction or occupied-roof
checks, and cannot enable arbitrary unlisted carriers.

## Artillery turret found after the Battle Fortress hint

`mods/ca/bits/artytur.shp` is still present. `sequences/allies.yaml` binds it as
`batf.turret` and both `batf.artillery.turret` / `turret2`, with 32 facings and
`UseClassicFacings: False`. It is a separate long-barrel artillery turret, not
part of the hull. The old base BATF is marked unused, but the artwork remains.

Raw frames were exported through the same engine Utility and inspected in
`artillery-turret-preview.png`. This identifies the asset the user recalled and
makes it a concrete artillery/mortar-carrier candidate. Overlord roof size,
mount/muzzle offsets, layering and palette still require a live composite check.
Reusing Allied artwork does not automatically grant China Allied weapon modules;
visual compatibility and gameplay faction availability are separate decisions.

Reproduction: export `bits/artytur.shp` as above, then run the preview script with
`--sprites artytur`. No carrier cost, faction permission or engine rule is changed
by this inspection.

## Follow-up: opt-in mortar drafts

The offline catalog now contains `mortar-roof-carrier`, `roof-mortar` and
`mortar-he`, with the artillery sprite as the proposed visual. Three draft designs
retain the Overlord main cannon. The package is provisionally priced at 650
credits before any defense additions; there is no original equivalent vehicle
and no claim of balanced combat power. See the latest README and workbook.

Both `experimental_carriers` membership and explicit design draft opt-in are
required. Geometry and rendering have not been validated on a moving Overlord.
The existing projectile/warhead definitions will be reused by a future compiler;
adding this numerical package is not executable YAML generation.

## Earlier China-only search: mortar rules but no identified mount

`weapons/ballistics.yaml` defines `MortarPrototype` (reload 60, range 11c0,
minimum range 1c0, arcing BulletCA) and `Mortar`/`MortarE`. Chinese `Mortar`
infantry uses those weapons, but its `mortar.shp` is an infantry sequence, not
proof of a separated armored roof turret.

No dedicated mortar turret was identified in the China sequence/vehicle files
inspected here. This is NOT an exhaustive claim that the repository contains no
usable mortar artwork. A separate model or adapted asset should be selected
before enabling the mount; no arbitrary weapon/price placeholder is added yet.

A mortar loadout must retain the full existing projectile/warhead package. It
must not masquerade as a mere reskin of the Gatling assembly. Likewise the flame
fuel currently references existing lingering-fire effects; direct-only flame and
other payload variants require explicit template composition, not color swaps.

## Reproduce the contact sheet (optional developer tool)

Build `engine/OpenRA.Utility/OpenRA.Utility.csproj` in Release. In a scratch output
directory invoke `dotnet <engine>/bin/OpenRA.Utility.dll <game>/mods/ca --png
<sprite> <game>/mods/ca/bits/palettes/temperattd.pal --noshadow`, setting ENGINE_DIR
to the absolute engine directory and MOD_SEARCH_PATHS to game/mods and engine/mods.
Export both `bits/generals/chdragontur.shp` and `bits/generals/chgatlingt.shp`.

Then run (Pillow is optional, only needed for this preview tool):

```sh
python -m pip install Pillow
python tools/modular/make_roof_preview.py <scratch-output> docs/modular/roof-sprite-preview.png
```

Do not write converted frames into the live mod sequence directories.
