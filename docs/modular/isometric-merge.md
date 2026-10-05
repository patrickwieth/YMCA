# Integration of origin/isometric into modular

Merged isometric `3ca145bd` (including topology-first maps/editor/bridges `1eb5e1bb`)
into modular after `2af69e6f`. Backup ref: `backup/modular-before-isometric-2af69e6f`.

## Conflict resolution

- Main menu uses `MainMenuLogicCA, CustomFactionMenuLogic`: normal skirmish opens the new wizard.
- Designer calls `MainMenuLogicCA.StartConfiguredSkirmish(uid)` directly, not the wizard button.
  Frozen designer maps keep their UID and ordinary connection/lobby lifecycle.
- Both `maps/ca/generated` and `maps/ca/modular` remain registered.
- The single registered server trait remains `CustomFactionSkirmishLogic`. Non-designer
  sessions now delegate to `SkirmishLogicCA`, preserving wizard map selection and the
  upstream explicit local/dedicated terrain diagnostic behavior. Designer sessions retain
  their no-restore/no-save settings guard.
- Ground and air/naval research snapshots refreshed from the merged resolved rules.
- Four offline locomotor snapshots explicitly gain upstream Dirt/Sand terrain speeds
  from `rules/world.yaml`: wheeled 75/50; tracked/lighttracked/sheavytracked 88/88.
  No component price, mass, power or reference-unit calibration was refitted; no Excel regeneration.
- Protected local `mods/ca/bits/nod/buildings/conyard.png` was neither modified nor staged.

## Validation

- Release solution build: successful (four existing warnings).
- Modular native tests: 180 passed; tournament bot tests: 13 passed.
- Designer catalog and all 168 numeric fixtures unchanged from pre-merge HEAD.
- Modular Python tests: 111 passed, including a new menu/server wiring regression guard.
- Resolved engine preview/baseline check: all 89 bindings passed.
- These checks do not establish live terrain rendering, wizard UI behavior, combat or network correctness.
- No push, release or deployment.
