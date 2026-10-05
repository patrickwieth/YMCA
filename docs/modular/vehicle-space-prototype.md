# Unified vehicle designer / spatial prototype

The faction designer is now **one view**. The separate Space designer button/window has been removed.
Faction selection, faction library loading, naming, New/Copy/Remove, templates, Save and Test Game remain.
The 1120×740 panel has a left sidebar, central schematic, and the original rotating preview on the right.

## Progressive configuration

- A new design initially shows **Chassis** only.
- Choosing a valid chassis reveals **Turret / mount**. Only the compiler's explicitly compatible original mounts are offered.
- Choosing the mount reveals **Engine, Running gear, Weapon, Ammunition, Armor, Free modules / generator**.
- Stock components are arranged as illustrative blocks when the mount is selected. Loaded/copied designs restore their configured parts and reveal their existing selections immediately.
- These are the actual faction/chassis choices from the catalog, not the old two-vehicle demo selector.
- Each design has its own temporary layout, keyed by the design object (not its mutable name or a cross-faction ID).
  Switching factions/loading another roster cannot reuse another design's layout. Removed designs are discarded from that cache.

## Side-view schematic and cursor interaction

Hull and turret are shown from the **left side, front facing right**. The tank outline is independent
of the rectangular grid: sloped hull nose, rear deck, track loop/road wheels, angular turret, hatch,
antenna, turret ring and forward gun barrels. The turret docks above the hull.
The schematic is generic, not a claim that every original actor uses tracks or a rotating turret.

- Select a component on the left to carry its multi-cell footprint at the cursor.
- Over a grid it snaps to cells: green outside border = valid; red = invalid.
- Click to drop; **R** rotates; **Esc**, Cancel or right-click cancels a held item.
- Selecting a native replacement picks up the existing block for that role rather than creating an extra engine/weapon/etc.
- Dropping a compatible native part applies it to the configured stock design, refreshing the **preview and properties**.
  Failed validation restores the previous native parts; failed placement leaves the previous block in place.
- Moving an existing block changes its temporary position only. Cancel restores its original visible position.
- Right-click an installed block with an empty hand to remove the layout block. This does **not** delete the underlying stock part.
- Chassis changes reset the temporary layout and require a mount selection again.

## Color-coded placement rules

| Color | Zone | Rule |
|---|---|---|
| Yellow | Bottom hull row | Running gear is **4×1**, entirely in this row. Other modules cannot use it. |
| Red | Right/front turret column | Weapons must touch the front edge; the rest of their footprint may occupy turret interior. Other modules cannot use red cells. |
| Blue | Hull top/front/rear perimeter | Armor strips fit only at the edge; rotate a 2×1 strip for vertical edges. Bottom cells remain reserved for running gear. |
| Gray | Interior | Engine, generator, ammunition, batteries and PDL subject to their hull/turret restrictions. |

The matching sidebar fields/options use the same colors. These constraints apply to the **prototype layout**, not to new compiled physical dimensions.

## Right-hand preview and properties

The original sprite/voxel preview and rotation control remain. Beneath them, a scrollable vertical list shows
price, HP, speed, turn speed, mass, technology, experimental electrical demand/drive reserve, development
budget, CP, base faction, production/graphics notes, all configured parts and the original weapon/ability summary.
These describe the configured stock design, **not** an unimplemented mixed actor or an electrical simulation.

## Persistence and honest limitations

Existing roster/library serialization, narrow legacy-ID migration and immutable map compilation are retained.
Native part selections are saved as before. Grid coordinates, silhouette geometry and planning-only blocks
are **not saved or compiled** yet; the window states this explicitly.
Battery, PDL and Reflector blocks are labelled **planning**. If any cached current-roster layout includes them,
Save/Test Game asks for confirmation before proceeding with **configured stock parts only**. There is no
new energy consumption, charging logic, armor coverage model or permission for arbitrary turret swaps.

Grid sizes and module dimensions remain illustrative. The current generic hull templates use 8×4 or 10×5
(selected at chassis initialization from the provisional configured mass), and a 3×4 mount interior. These
are not calibrated budgets, and do not override Battle Fortress's three-slot/full-width Bunker policy.

Model: `OpenRA.Mods.CA/Modular/CustomVehicleSpaceDemo.cs`.
Drawing/input/held-item overlay: `Widgets/CustomVehicleSpaceWidget.cs`.
Embedded sidebar controller: `Widgets/Logic/CustomVehicleSpaceLogic.cs`.
Main roster UI: `Widgets/Logic/CustomFactionLogic.cs`, `mods/ca/chrome/custom-faction.yaml`.

Automated checks cover placement/zone rules, stock-only reconstruction, original compiler/roster behavior,
preview binding and unified UI wiring. The new visual layout and input flow still need live acceptance testing.
