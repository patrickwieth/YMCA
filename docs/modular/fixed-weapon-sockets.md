# Fixed weapon sockets: first working grid implementation

The old 2×2 weapon block and decorative barrel outside the grid are removed. The **weapon drawing
itself fills its red socket cells**, including while picked up. All cells remain **28×28 pixels**.

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
- Weapons must match the **entire socket footprint and anchor**, not merely touch its red edge.
  Rotating these horizontal weapons produces an invalid vertical fit. Cancel preserves the original.
- Hovering any cell of the matching socket snaps a held weapon to the socket origin. The protruding
  cells are rendered, hit-tested and pickable, just like the cells inside the turret.
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

**Agreed next weapon variant:** a 120mm Smoothbore Cannon for the Battle Tank Cannon Turret,
with one additional forward cell (5×1), not an extra interior slot. This is not yet selectable:
its increased damage and other provisional allocations need to be defined before adding a reviewed
compiler binding. Do not infer changed caliber or damage from the existing stock weapon's internal
`120mm` ID; the existing 105mm display designation retains its original package.

**Still pending:** Mammoth Mk. II's single four-legged platform with two independent cannon sockets
and one missile-package socket, and further source-specific socket definitions. Its stock railgun
and ground/air missile channels must not be duplicated when those sockets are introduced.

Tests cover exact fit, protruding-cell hit geometry, invalid outside cells, occupancy, rotated/failed
moves, Sonic's internal socket, all schematic kinds and stock reconstruction. Live visual acceptance
is separate from these tests.
