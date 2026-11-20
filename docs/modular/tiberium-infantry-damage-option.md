# Optional Tiberium infantry damage

Local Game / isometric follow-up after v0.97.5; not committed, pushed or released.

Lobby checkbox **Tiberium Infantry Damage**, ID `tiberiumdamage`, defaults to **off**.
It is visible and unlocked. It uses the existing synchronized lobby prerequisite
mechanism (`global.tiberiumdamage`), not a local settings preference. Hosts can enable
it for the match; omission of the option also means off.

`^BiologicalDamage` grants `tiberium-damage-enabled` from that prerequisite and gates
only `DamagedByTerrain@TIB`. Both green and blue Tiberium use the existing 100 damage
per 16 ticks when enabled. The hazmat and being-warped conditions are preserved,
as are cyborg immunity (`^BioDamageImmune`), Scrin's removed damage trait, and existing
Tiberium healing. Radiation, toxins, other terrain damage, resource density and
vehicle behavior are not changed.

## Validation

- Resolved checkbox: default-off, visible, unlocked, correct prerequisite.
- Resolved damage: unchanged amount/interval; expression checks cover enabled/off,
  hazmat and being-warped cases.
- Three isolated native simulations: absent/default, explicit on, explicit off.
  Normal infantry on green/blue loses health only with the option on. Clear-ground
  infantry, hazmat-protected infantry, Nod cyborg, Scrin infantry and vehicle reference
  cases retain health in all three runs.
- The opt-in fixture sets the real `Session.Global.LobbyOptions` entry before player
  creation, so native `LobbyPrerequisiteCheckbox` / tech-tree condition wiring is
  exercised; no substitute damage trait is used. This fixture injection is diagnostic
  only and is never used to alter a real lobby.
- Evidence: `art-source/tiberium-damage-option/` from `.tmp/tiberium-{default,on,off}02`.

```
powershell -File tools/test-tiberium-damage.ps1 -EngineDirectory ENGINE -OutputDirectory FRESH_PATH -Mode default
# Repeat with fresh paths and -Mode on / -Mode off.
```

Requires `YMCA_TIBERIUM_DAMAGE_PROBE=1`, set/restored by the isolated launcher.
