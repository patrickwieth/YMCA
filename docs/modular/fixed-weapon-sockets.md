# Fixed weapon sockets: first working grid implementation

The old 2×2 weapon block and decorative barrel outside the grid are removed. The **weapon drawing
itself fills its red socket cells**, including while picked up. All cells now use **96×96 UI pixels**; footprint counts and socket rules are unchanged.

## Geometry and input

- **Battle Tank Cannon Turret** belongs to **Medium Tank Turret**, admitted by the GDI Battle Tank Hull.
  Its standard **105mm Smoothbore Cannon** uses **4×1 fully external cells**, starting at `(5, 1)`.
  The turret silhouette stops at that boundary too. Carrier/weapon IDs and combat values are unchanged;
  the shared Challenger binding retains the same carrier and gets its corrected labels and geometry.
- Other initial forward sockets are **4×1**, at turret-local `(4, 1)` for the 5-column turret: one cell
  inside the body and three cells protruding forward. Only that row exists beyond the turret body;
  this does not create extra storage columns.
- **Sonic Turret** uses a **5×1** socket at `(0, 0)`, entirely inside the top row. Ammunition restores
  below it, not over the emitter.
- Weapons must match their **mount-specific footprint and anchor**, not merely touch a red edge.
  Socket geometry is fixed; a larger weapon may require additional occupied cells inside the turret.
  Rotating these horizontal weapons produces an invalid vertical fit. Cancel preserves the original.
- Hovering any cell of the matching socket snaps a held weapon to the socket origin. The protruding
  cells are rendered, hit-tested and pickable, just like the cells inside the turret.
- Standard tank shells (`medium-tank-shell`) and high-explosive shells (`designer-he-shell`) now
  occupy **2×2 cells**, including held/rotated footprints and restored stock layouts. Other ammunition
  keeps its existing 1×2 allocation. A 2×2 shell block cannot overlap the 120mm's interior breech;
  ammunition capacity, damage, price and mass are unchanged.
- Ammunition uses only violet interior cells. Battery/PDL retain their color exception inside a
  container, but cannot occupy protruding weapon-only cells. They can block a weapon if placed over
  the in-body part of its socket; collision checks still apply.
- Removing a layout block removes its schematic barrel. It does not remove the configured stock
  weapon package or alter the original preview/gameplay; the prototype's stock-only warning remains.

## Bound schematic artwork

Carrier IDs choose the schematic: cannon/twin barrels, **Dual Gatling**, **Triple Ion Cannon**,
missile pods, **Sonic emitter** or prism emitter. These drawings follow the held footprint when
rotated. Known mappings include the Vulcan, MARV, Hover MLRS, STNK, MLRS, Disruptor and Prism tank.
Unreviewed carriers still use the generic 4×1 cannon schematic, not a claim of exact stock artwork.
The right-hand original actor preview remains the visual reference.

The actual weapon dropdown still uses `CompatibleOptions` and the native compiler. These grid
shapes do not admit arbitrary weapon/turret combinations, change weapon values, add firing channels
or save grid placements. Current layouts represent one bound weapon package per mount.

**Implemented alternative:** the **120mm Smoothbore Cannon** (`battle-tank-120mm`) is selectable
on the GDI Battle Tank only. **Turret geometry and its four external socket cells remain identical**
when switching weapons. The 105mm occupies `(5, 1)` through `(8, 1)`. The 120mm occupies `(4, 1)`
through `(8, 1)`: its fifth cell is the breech **inside the turret**, consuming space otherwise
available to ammunition, batteries or PDL. It does not add exterior cells or move the turret outline.
Picking it previews that interior footprint; an occupied breech cell prevents the drop and native
part commit. Cancel restores the original weapon footprint and all placements. Switching back to
105mm frees the interior cell. Stock reconstruction restores the 120mm at its correct inward anchor.

The user-approved provisional increase is **+20% primary damage**, for both existing AP and HE
ammunition. Reload, range, burst, projectile, versus tables and effects remain inherited/unchanged.
Price, mass, electricity, tech and catalog points provisionally equal the 105mm allocations; they
have not been independently rebalanced. Shared Challenger/Soviet weapons do not gain this option.
The 105mm remains the default, existing saved profiles and frozen maps remain unchanged, and the
original actor preview still uses stock artwork (the extended barrel is a grid schematic).
The designer fixture now has 192 combinations instead of 168; no broad calibration/Excel changes.
Do not infer caliber from the stock weapon's internal `120mm` ID: both designer variants inherit
that original package (or `120mmHEAT`), with only the selected alternative's damage increased.

**Still pending:** Mammoth Mk. II's single four-legged platform with two independent cannon sockets
and one missile-package socket, and further source-specific socket definitions. Its stock railgun
and ground/air missile channels must not be duplicated when those sockets are introduced.

Tests cover exact fit, protruding-cell hit geometry, invalid outside cells, occupancy, rotated/failed
moves, Sonic's internal socket, all schematic kinds and stock reconstruction. Live visual acceptance
is separate from these tests.
