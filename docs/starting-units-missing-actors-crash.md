# Missing e1/n1 in starting-unit groups (v0.96.20)

Reported on Rapa Nui, map UID aaaf8b9ae948dfcea1fff4913edd8848bc44fe9d,
CA v0.96.20 / engine f31038bf1f3ccecb20e55eaa281288d6ec430360.
SpawnStartingUnits.SpawnUnitsForPlayer fails while looking up a SupportActors entry named `e1`.
This is independent of the mind-control crash and occurs during world loading.

## Cause and correction

The Soviet light/heavy start groups still reference the removed actor `e1` three times each.
The GDI light/heavy groups have the same problem with removed actor `n1`.
Replace these with the current basic infantry actors:

- Soviet: `conscript` (Russia, North Korea, Iraq and legacy Soviet faction ID).
- GDI: `mercenary` (Eagle, Talon, ZOCOM and legacy GDI faction ID).

No changes to unit counts, other supporting units, MCVs, proxy actors, spawn radii or balance.
Do not paper over the error by skipping missing units or creating a generic e1 alias.
Both affected defaults files were identical between master and v0.96.20 before these corrections.
The fix is in shared faction rules, not specific to Rapa Nui. Third-party map overrides can still
contain their own obsolete actor IDs and require separate checking.

## Regression check

New utility command:

```
dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-starting-units
```

It validates resolved WORLD starting groups, including BaseActor, SupportActors, SupportBuildings
and SupportProxyActors. Supporting units must have placement traits and valid mobile locomotors;
supporting buildings must have BuildingInfo. Abstract actors are rejected.

Before correction: 12 invalid references reported (six e1 and six n1).
After correction: all **24 groups / 128 actor references** pass.
The extended check also verifies **18 selectable concrete factions x 4 start classes = 72
combinations**, plus all **16 random-faction selections**, including nested selections. It rejects
unknown/non-selectable random members, cycles, paths exceeding the engine's random-resolution
limit, missing faction/class groups and an undefined default starting class.

Allies (England/France/Germany), Soviets (Russia/North Korea/Iraq), GDI (Eagle/Talon/ZOCOM),
Nod (Black Hand/Marked/Legion), China (Infantry/Tank/Nuclear) and Scrin (Reaper/Traveler/Harvester)
all have valid MCV Only, Double MCV, Light Support and Heavy Support definitions after the fix.
No additional missing starting actors were found outside the Soviet/GDI references already fixed.

The existing full mind-control
audit still passes (1557 actors / 4 controller weapons). Solution build succeeds.
This is resolved-rule validation, not a claim that the supplied Rapa Nui match was replayed;
no replay or exact map package was supplied for this report.

Before release, smoke-test Light Support and Heavy Support with Soviet and GDI factions,
including their random-faction paths. Carry this fix onto the v0.96.20 release line along with
the mind-control fixes; master itself is not the current released game baseline.
