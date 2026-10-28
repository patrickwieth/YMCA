# Gene Splicer zombie conversion

## Cause

GeneSpliceBeam inherited damage warheads from ^Radiation before its genespliced condition
warhead. A lethal first hit could therefore kill an unmarked infantry victim. ZombieAttack had
the same ordering problem through ^Bullet.

Victims with the active condition spawn zombie.mutating via ^GeneticallyMutatable. That new actor
does not inherit the victim's runtime conditions, but its SpawnActorOnDeath@Zombie still inherited
RequiresCondition: genespliced. The transition to the final zombie was therefore disabled.

## Correction

- Shared ^GeneSpliceEffect is inherited before ^Radiation/^Bullet. Resolved condition warheads
  now precede every damage warhead, including on all four Gene Splicer beam variants.
- ZombieAttack retains its existing shorter condition duration (10 ticks versus 100 for the beam).
- Zombie.Mutating explicitly removes the inherited RequiresCondition on its final-zombie spawn.
  The actual infantry victim still requires genespliced; unmarked deaths do not produce zombies.
- Existing make animation, delayed self-kill, killer -> mutation -> zombie ownership chain,
  damage, ranges, burst, reload, marker radius, probabilities and defeat behavior remain intact.

This introduces no random conversion chance. The existing spawn probability is 100% for eligible
marked deaths, not for every infantry unit killed anywhere in the damage area. The marker's
existing 0c256 radius and duration are unchanged.

## Validation

```
dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-gene-splicer
```

Checks resolved warhead order, unchanged values, ownership/conditions/probability of both spawn
stages, and final zombie movement/armament. The base beam, all four firing variants and zombie
melee pass. Solution build succeeds. These are headless rule checks, not a completed live
animation/kill regression test.

Recommended live cases before release: lethal first beam hit, several nonlethal hits then death,
lethal zombie melee, unmarked death, expired marker, and correct zombie ownership after mutation.
Suggested player-facing release note: "Fixed Gene Splicer victims failing to turn into zombies."
No release is created for this fix alone.
