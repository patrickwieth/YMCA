# Side-view vehicle inventory prototype

Open **Eigene Fraktion → Space designer**. All text in the space-designer window is English.
This remains a temporary UI experiment: closing it leaves the roster, library, compiler and saved maps untouched.

## Selection and interaction

The left sidebar is ordered **Chassis, Turret, Engine, Running gear, Weapon, Ammunition, Armor, Free modules**.
Start by choosing a chassis. The turret selector is disabled until a chassis exists. Two sample hulls
(Battle Tank 8×4, Mammoth 10×5) and two sample turrets (3×4 and 4×4) are available.
Chassis and turret selections change the structure directly; other selections put an item **in your hand**.

- Hull and turret are shown from the **left side, front facing right**. The turret docks above the hull.
- Held modules follow the mouse as a full multi-cell footprint with dimensions, e.g. **2×2**.
- Over a grid, the item snaps to cells: green outside border = valid, red = invalid.
- Click to drop. Press **R** or use Rotate to turn the footprint. **Esc**, Cancel or right-click cancels a held item.
- Click an installed item to pick it up. It disappears from the original drawing while in hand, but its
  original model placement is retained until a valid drop succeeds. Cancel restores its visual position.
- Right-click an installed item with an empty hand to remove it.
- **Load example layout** creates a zoned sample with engine, generator, tracks, battery, armor, cannon,
  ammunition and PDL. It can also initialize the first chassis.
- Changing chassis clears the temporary layout. Changing turret clears only turret contents.
- Free modules currently include generator, battery and PDL. PDL becomes selectable once a turret exists.
- The small rotating model is an explicitly labelled **stock reference**, not the hypothetical inventory assembly.

## Color-coded placement rules

| Color | Zone | Enforced rule |
|---|---|---|
| Yellow | Bottom hull row | Running gear is **4×1**, must fit entirely in this row. Other modules cannot use it. |
| Red | Right/front turret column | A weapon must touch the front edge. Its remaining footprint may extend into turret interior. Other modules cannot occupy the red cells. |
| Blue | Hull top, front and rear edges | Armor strips occupy only perimeter cells; rotate a 2×1 strip for vertical edges. The bottom row is reserved for running gear rather than armor. |
| Gray | Interior | Engine, generator, ammunition, batteries and PDL, subject to their hull/turret restrictions. |

The sidebar labels and corresponding choices use the same yellow/red/blue colors. These are actual
placement constraints, not just decorations. Cannon barrels are drawn at the installed weapon's height,
projecting forward from the turret. PDL and Reflector armor have no mutual-exclusion rule.

## Scope and limits

Silhouettes, cell counts, module dimensions and zone restrictions are **illustrative, not calibrated**.
Reflector is demonstrated as an armor-strip choice, not a newly balanced coverage/damage system.
There is no profile persistence, actor compilation, battery charging or per-shot energy simulation here.
A fitted PDL without any battery triggers a planning warning only. Multiple batteries are permitted.
Battle Fortress's existing three-slot/full-width Bunker policy is not changed by these sample layouts.

Model: `OpenRA.Mods.CA/Modular/CustomVehicleSpaceDemo.cs`.
Renderer/input and non-intercepting held-item overlay: `Widgets/CustomVehicleSpaceWidget.cs`.
Sidebar/window: `Widgets/Logic/CustomVehicleSpaceLogic.cs` and `mods/ca/chrome/custom-vehicle-space.yaml`.

Tests cover docking prerequisites, exact zone restrictions, edge precedence, rotation, collisions,
atomic movement between containers, duplicate batteries, PDL/Reflector coexistence and reset behavior.
Live UI/art acceptance still needs review before assigning real chassis budgets or saving layouts.
