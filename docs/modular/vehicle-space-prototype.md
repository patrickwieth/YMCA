# Unified vehicle designer / spatial prototype

There is **one vehicle designer**, reached from the [faction overview](faction-navigation.md).
The separate Space designer button/window has been removed. Faction creation is under Play; faction
loading, naming, roster management and Test Game are now owned by the library/overview.
The 1120×740 vehicle panel has a left sidebar, central schematic, and the original rotating preview on the right.

## Progressive configuration

- A new design initially shows **Chassis** only.
- Choosing a valid chassis reveals **Turret**. Only the compiler's explicitly compatible original mounts are offered.
- Choosing the mount reveals **Engine, Generator, Running gear, Weapon, Ammunition, Armor, Free modules**.
- Stock components are arranged as illustrative blocks when the mount is selected. Loaded/copied designs restore their configured parts and reveal their existing selections immediately.
- These are the actual faction/chassis choices from the catalog, not the old two-vehicle demo selector.
- Each design has its own temporary layout, keyed by the design object (not its mutable name or a cross-faction ID).
  Switching factions/loading another roster cannot reuse another design's layout. Removed designs are discarded from that cache.

## Side-view schematic and cursor interaction

Hull and turret are shown from the **left side, front facing right**. The tank outline is independent
of the rectangular grid: sloped hull nose, rear deck, track loop/road wheels, angular turret, hatch,
antenna, turret ring and forward gun barrels. The turret docks above the hull. Mount selection is only in the sidebar; there is no extra dock button or white box on the silhouette.
[Class-specific silhouettes](faction-navigation.md#silhouettes) now replace the all-tank fallback for
explicit walker, light vehicle, wheeled, motorcycle and native-hover bindings. Light-vehicle mounts
are drawn smaller, including a matching smaller grid display scale, without altering slot counts.
These remain schematics, not exact reproductions of each stock actor or proof of arbitrary mixed art.

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
| Yellow | Bottom hull row | Running gear is **4×1**, entirely in this row. |
| Red | Right/front turret column | Weapons must touch the front edge; the rest of their footprint may extend into violet turret cells. Ammunition cannot use red cells. |
| Blue | Hull top/front/rear perimeter | Armor strips fit only at the edge; rotate a 2×1 strip for vertical edges. Bottom cells remain reserved for running gear. |
| Green | Hull interior | Engine and generator must fit entirely inside this zone. Both have green sidebar fields. |
| Violet | All non-red turret cells | Ammunition belongs here, not in the hull or across the red weapon dock. |

**Free modules** (battery and PDL) ignore zone colors and fit in either hull or turret, including across
zone boundaries. They still require free, in-bounds space and block other items normally. Generator is
now a separate green category, not a free module. Reflector remains in the blue armor category.

The matching sidebar fields/options use the same colors; no separate legend is shown under the schematic.
[Military pixel-art module icons](module-icons.md) appear in selectors, option menus, fitted blocks and
cursor-held items. English names remain in the sidebar/selection line. The redundant FRONT arrow label is removed. These constraints apply to the **prototype layout**, not to new compiled physical dimensions.

## Right-hand preview and properties

The original sprite/voxel preview and rotation control remain. Beneath them, a compact scrollable list uses
**`Property: value` on the same line** (for example `Hitpoints: 45000`); only long descriptions wrap. It shows
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
(selected at chassis initialization from the provisional configured mass), and a **5×3 mount interior**. These
are not calibrated budgets, and do not override Battle Fortress's three-slot/full-width Bunker policy.

Model: `OpenRA.Mods.CA/Modular/CustomVehicleSpaceDemo.cs`.
Drawing/input/held-item overlay: `Widgets/CustomVehicleSpaceWidget.cs`.
Embedded sidebar controller: `Widgets/Logic/CustomVehicleSpaceLogic.cs`.
Main roster UI: `Widgets/Logic/CustomFactionLogic.cs`, `mods/ca/chrome/custom-faction.yaml`.

Automated checks cover placement/zone rules, stock-only reconstruction, original compiler/roster behavior,
preview binding and unified UI wiring. The new visual layout and input flow still need live acceptance testing.
