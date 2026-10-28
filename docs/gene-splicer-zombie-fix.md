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
- Zombie.Mutating removes the inherited source-tracking spawn trait and uses an unconditional
  standard SpawnActorOnDeath for its final zombie. The actual victim still requires genespliced.
- GeneSpliceWarhead records each successfully granted external-condition token and the source
  player at application time. SpawnActorOnDeathFromConditionSource uses the latest still-valid
  token's owner, never the player who lands the killing blow. The intermediate actor preserves
  that ownership when becoming the final zombie.
- If the latest mark expires or is revoked, an older still-active mark applies; with no active
  tracked mark, no zombie spawns. ExternalCondition remains responsible for all token lifetimes
  and revocation (including capture). Source death/capture does not rewrite recorded player identity.
- Existing make animation, delayed self-kill, damage, ranges, burst, reload, marker radius,
  probabilities and victim-defeat behavior remain intact. Friendly infantry can still be marked;
  the resulting zombie belongs to the infecting player, not the opposing killing player.

This introduces no random conversion chance. The existing spawn probability is 100% for eligible
marked deaths, not for every infantry unit killed anywhere in the damage area. The marker's
existing 0c256 radius and duration are unchanged.

## Validation

```
dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-gene-splicer
```

Checks resolved warhead order, unchanged values, ownership/conditions/probability of both spawn
stages, and final zombie movement/armament. Also tests source tracking with multiple active marks,
revocation/expiry, invalid tokens and reinfection. The base beam, all four firing variants and zombie
melee pass. Solution build succeeds. These are headless rule/source-tracker checks, not a completed
live animation/kill regression test.

Recommended live cases before release: lethal first beam hit, several nonlethal hits then death,
lethal zombie melee, unmarked death, expired marker, and correct zombie ownership after mutation.
Specifically test friendly infantry marked by a player's beam then killed by the opponent, enemy
infantry killed by a third player after infection, source death/capture, and competing infections.
All resulting zombies must belong to the most recent still-active infection's original source player.
Suggested player-facing release note: "Fixed Gene Splicer victims failing to turn into zombies."
No release is created for this fix alone.
