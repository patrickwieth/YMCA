# Vehicle module icons

## User-provided engine and generator portraits

The user-provided square **RGBA** PNGs are retained without modification by the icon generator.
The current sources are 256×256, except the user-updated improved diesel at 192×192:

| Source under `mods/ca/bits/modular/` | Bound component |
| --- | --- |
| `engines/basic diesel.png` | `diesel` — Standard Diesel Engine |
| `engines/improved diesel.png` | `diesel-large` — High-Output Diesel Engine |
| `generators/basic generator.png` | `baseline-generator` — Baseline Generator |

The enlarged designer now uses **64×64 UI-pixel cells**. A 2×2 module occupies **128×128 UI pixels**,
with about 120×120 pixels available for its icon. OpenRA's UI scaling applies additionally. Large grid/held icons use the **1024×256**
`component-icons-large.png` atlas (256px tiles), generated directly from the source PNGs. The
192px source is resampled, not rewritten. Menus keep the prefiltered **256×64** `component-icons.png`
atlas (64px tiles). Both atlases have power-of-two dimensions and a transparent fourth column.
The renderer selects the larger atlas above 64px, so it never stretches a small menu icon to fill
an enlarged module. The remaining generic category art intentionally retains its pixel-art scale.

Run `python tools/modular/generate_component_icons.py` after updating the source PNGs.
This does not regenerate or overwrite the separate generic category artwork below.

The selected component's portrait appears in the sidebar, dropdown, fitted grid block and held
item. A pending replacement uses its own portrait; cancelling restores the configured component's
portrait. Components without a specific portrait keep their generic category icon. No module
footprints, stats, saved IDs or bindings change. Automated checks cover the sources, generated
pixels, chrome regions and UI wiring; final small-icon readability needs live review.

## Original category set

![Module icon review sheet, enlarged 3x with nearest-neighbor sampling](module-icons-preview.png)

Nine original **64×48 RGBA PNGs**, plus a **256×256 runtime atlas**, in `mods/ca/bits/modular/`.
Chrome textures require power-of-two dimensions: the atlas keeps its 192×144 icon region in the
upper-left corner, with transparent padding and unchanged chrome coordinates. Individual source
icons and the documentation review sheet are not uploaded as standalone chrome textures.

Icons:

- `weapon.png`: smoothbore gun, breech and mount
- `engine.png`: armored diesel engine block
- `generator.png`: alternator, cooling fan and control box
- `gear.png`: heavy tank treads and road wheels
- `ammo.png`: brass rounds in an ammunition crate
- `armor.png`: layered armor plates
- `battery.png`: military power pack
- `pdl.png`: point-defense laser emitter
- `reflector.png`: faceted reflector armor

Style references inspected: `bits/gdi/vehicle_armor.png`, `bits/gdi/energy_weapons.png`,
`bits/nod/improved_lasers.png`, `bits/allies/armoreddoctrineicon.png`. Their commander-tree format is
64×48: dark contours, olive/steel hardware, strong specular highlights and small colored accents.
The new artwork uses hand-authored integer-pixel shapes and material palettes. No original artwork
was copied, altered or used as a generation input. Transparent backgrounds keep the slot colors legible.
This first set covers **module categories**, not a distinct portrait for every supported stock component.

The icons appear on component selectors, in wider option menus, on fitted blocks and on cursor-held
items. Slot outlines and held-item dimensions remain visible; icons stay upright when footprints rotate.
English names remain in the sidebar and selection line. Icon widgets do not intercept mouse input.
No actor spawning, palette changes, electrical simulation or calibration changes are involved.

Regenerate the PNGs, atlas and review sheet with:

```
python tools/modular/generate_module_icons.py
```

Pillow is the only artwork dependency. Source definitions remain editable in the generator.
Tests compare generated pixels, atlas regions, transparency and module coverage. Ingame sizing/style
still needs live review; the review sheet is not a screenshot of the running designer.

Related UI changes in this pass: `Standard Tracks` is now **Heavy Tank Treads** (the shared component's
label only, not a locomotor change), and the schematic's `FRONT -->` text has been removed.
