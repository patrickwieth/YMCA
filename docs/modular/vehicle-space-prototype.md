# Vehicle space / inventory prototype

Open **Eigene Fraktion → Bauraum ausprobieren**. This is a separate, temporary UI experiment;
closing it leaves the faction roster, library, compiler and saved maps untouched.

## Interaction

- Choose Battle Tank or Mammoth as a sample chassis. The schematic hull changes from 4×5 to 5×6.
- Click the gold turret dock (or its dropdown) to choose no turret, a 3×4 turret, or a 4×4 turret.
- The hull and turret each have their **own inventory**. The dock is separate from interior cells.
- Hull examples: **running gear, engine, generator, batteries and Reflector**.
- Turret examples: **weapon, ammunition, PDL and batteries**. Ammunition can also be placed in the hull.
- Select a module button, then click its top-left grid cell. Rotate using the 90-degree button.
- Clicking a fitted module selects it for movement. Its old position remains occupied until a valid move succeeds.
  Cancel leaves it in place. Right-click a fitted module to remove it.
- Multiple batteries are allowed. Collision, bounds and the example hull/turret restrictions are checked.
- Green/red corner markers show whether the selected footprint fits at the pointer.
- The example starts with engine/generator/running gear/battery in the hull and weapon/ammunition/PDL
  in a 3×4 turret. **Beispielbelegung laden** resets the temporary layout to this example.
- Turret changes clear only turret contents; chassis changes clear the entire temporary layout.
- Missing battery with fitted PDL gives a planning warning, not a combat simulation.
- PDL and Reflector are not mutually exclusive in this experiment.

## Scope and limits

All silhouettes, cell counts, module sizes and placement restrictions are illustrative, **not calibrated**.
Running gear is an actual inventory item here; the drawn external tracks merely orient the schematic.
The rotating preview shows the **original stock reference vehicle**, not the hypothetical grid assembly.
No new actor/weapon/graphic combination is compiled or admitted. There is no persistence, charging,
shot-energy consumption, grid-based price/mass/CP change, or runtime electrical network.
This prototype does not change the existing three-slot Battle Fortress / full-width Bunker policy.

Model: `OpenRA.Mods.CA/Modular/CustomVehicleSpaceDemo.cs`.
Rendering/input: `Widgets/CustomVehicleSpaceWidget.cs`; window logic: `Widgets/Logic/CustomVehicleSpaceLogic.cs`.
UI: `mods/ca/chrome/custom-vehicle-space.yaml`.

Automated coverage includes hull hardware placement, docking, rotation, overlap/out-of-bounds rejection,
atomic movement, duplicate batteries, PDL/Reflector coexistence, resets and example layouts.
Live UI/art acceptance still needs user review. Decide final space budgets and persistence only after that review.
