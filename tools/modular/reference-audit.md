# Reference-source change audit

Previous: `7f5aeadf`; integrated: `9d8a9dec`.
Selected raw root blocks only; not a complete resolved-rules dependency or runtime audit.
Missing blocks stop the audit. Comments and blank lines are ignored.

| File / block | Status |
|---|---|
| `mods/ca/rules/china/vehicles.yaml` / `chbattle` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Autoloader` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Autoloader.PDL` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Autoloader.Reflector` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Nuclear_Shells` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Nuclear_Shells.PDL` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Nuclear_Shells.Reflector` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Mass_Production` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Mass_Production.PDL` | unchanged |
| `mods/ca/rules/china/vehicles.yaml` / `chbattle.Mass_Production.Reflector` | unchanged |
| `mods/ca/rules/china/weapons.yaml` / `CHBattlemasterCannon` | unchanged |
| `mods/ca/rules/china/weapons.yaml` / `CHBattlemasterCannon.Autoloader` | unchanged |
| `mods/ca/rules/china/weapons.yaml` / `CHBattlemasterCannon.Nuclear_Shells` | unchanged |
| `mods/ca/rules/china/weapons.yaml` / `CHAtomicTankExplode` | unchanged |
| `mods/ca/rules/china/defaults.yaml` / `^AtomicTank` | CHANGED |
| `mods/ca/rules/china/defaults.yaml` / `^UranShells` | unchanged |
| `mods/ca/rules/china/defaults.yaml` / `^HordeBonus` | unchanged |
| `mods/ca/rules/china/commander-tree.yaml` / `promotion.Battlemaster.Autoloader` | unchanged |
| `mods/ca/rules/china/commander-tree.yaml` / `promotion.Battlemaster.Nuclear_Shells` | unchanged |
| `mods/ca/rules/china/commander-tree.yaml` / `promotion.Battlemaster.Mass_Production` | unchanged |
| `mods/ca/rules/china/commander-tree.yaml` / `promotion.Battlemaster.PDL` | unchanged |
| `mods/ca/rules/china/commander-tree.yaml` / `promotion.Battlemaster.Reflector` | unchanged |
| `mods/ca/rules/gdi/vehicles.yaml` / `Juggernaut` | unchanged |
| `mods/ca/rules/gdi/vehicles.yaml` / `Juggernaut.Emp` | unchanged |
| `mods/ca/rules/gdi/weapons.yaml` / `JuggernautGun` | unchanged |
| `mods/ca/rules/gdi/weapons.yaml` / `JuggernautGun.Emp` | unchanged |
| `mods/ca/rules/gdi/weapons.yaml` / `JuggernautDummyAim` | unchanged |
| `mods/ca/rules/gdi/defaults.yaml` / `^GDIWalkerUpgrades` | unchanged |
| `mods/ca/rules/soviet/vehicles.yaml` / `TTNK.RA2` | unchanged |
| `mods/ca/rules/soviet/weapons.yaml` / `TTankZap` | unchanged |
| `mods/ca/rules/soviet/weapons.yaml` / `TTankZapMK2` | unchanged |
| `mods/ca/rules/soviet/defaults.yaml` / `^TeslaUnit` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^Vehicle` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^VehicleVision` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^Tank` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^FightingTank` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^FightingTankTurreted` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^PointLaserDefenseSystem` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^ReflectorArmor` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^HeavyArmor` | unchanged |
| `mods/ca/rules/defaults.yaml` / `^BigVehicle` | unchanged |
| `mods/ca/weapons/other.yaml` / `AdvancedPointLaser` | unchanged |

42 blocks compared; 1 changed.

## ^AtomicTank

```diff
--- 7f5aeadf
+++ 9d8a9dec
@@ -8,6 +8,9 @@
 	SpeedMultiplier@atomictank:
 		RequiresCondition: nucleartank
 		Modifier: 125
+	DamageMultiplier@isotopestability:
+		RequiresCondition: isotopestability
+		Modifier: 90
 	FireWarheadsOnDeathCA@atomictank:
 		Weapon: CHAtomicTankExplode
 		EmptyWeapon: CHAtomicTankExplode
```
