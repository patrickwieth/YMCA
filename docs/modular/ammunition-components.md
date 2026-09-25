# Ammunition packages and existing behavior

Agreed contract, not an implemented weapon compiler:

- Carrier: firing pattern, salvo timing, aiming and existing attack-state traits.
- Weapon: compatible ammunition and weapon-side projectile/range constraints.
- Ammunition: complete payload package, including projectile appearance/trail,
  impact damage/effects and persistent area/status effects. Not only a warhead.
- Compilation must explicitly compose these fields; no blind last-writer-wins
  merge or inference of damage type from projectile color.

Initial concepts (not yet priced/selectable new components):

| Package | Identity | Source pointers / caveat |
|---|---|---|
| Flame | Direct flame attack | Existing flame projectile and damage rules |
| Incendiary mixture | Flames plus lingering fire | `CHDragonFlamer` already includes FireCluster/BurnFx, heat and garrison damage; current reference fuel must retain these rather than silently becoming direct-only |
| Chemical mixture | Green chemical effect; Iraqi emphasis | `Chem_Spray` and its variants, soviet/weapons.yaml; inspect full inherited definitions before compiling |
| Acid | Armor corrosion; Scrin emphasis | `^CorrosionEffect` / ArmorCorrosion and `.Corrosive` variants in scrin/weapons.yaml |
| Toxin | Driver poisoning/killing; often purple | `^Toxin` in weapons/weapontypes.yaml includes DriverPoisonDamage against vehicles and separate infantry damage; not guaranteed instant driver death |

Color is a presentation choice. Green acid and green chemical projectiles do not
imply identical behavior. Corrosion is not automatically present on every legacy
acid-named weapon: select the actual corrosive variant/template. Faction allowlists
and compatibility must be explicit; emphasis is not a complete availability list.

Firewall activation/aiming remains carrier behavior. What those shots leave behind
belongs to the ammunition/effect package. Reuse existing traits and weapon rules.
Likewise reuse `^ChinaGatling` / `^ChinaGatlingOverlord` rather than implementing
spin-up again in the price calculator.

During composition, namespace trait instance IDs AND all linked armament/turret/
ammo/condition references. Build combined attack armament lists so PDL cannot
silently replace the main weapon or firewall armaments. Existing behavior is
already implemented; safe composition and regression checks are the remaining
integration work.
