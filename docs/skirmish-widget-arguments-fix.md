# Skirmish callback argument regression

The v0.97.2 wizard constructor added optional C# callback parameters, but the
engine's reflective `ObjectCreator.CreateUsingArgs` does not honor C# defaults.
Opening Skirmish from the main menu therefore failed with missing
`onGenerateRecipe`; local lobby and server-creation callers were affected too.
Dedicated callers supplied both callbacks, which is why that integration test
alone did not expose the regression.

Fix prepared in Game, Game-master and Game-release:
- All three entry points use `SkirmishSetupLogic.Open`.
- The shared factory copies the caller's arguments and explicitly supplies null
  defaults for both optional callbacks, preserving supplied remote delegates.
- The reflected constructor no longer advertises misleading C# defaults.
- `--check-skirmish-widget-arguments` checks every real constructor parameter and
  exercises the engine ObjectCreator with local and dedicated callback sets.
- The opt-in UI probe waits for shellmap startup to finish before clicking the
  real Singleplayer / Skirmish buttons. A fixed timer could previously race with
  the startup UI reset and close the test dialog.

Validation: native argument check passed against both the isolated engine and
Game's normal runtime. The full native main-menu/skirmish probe passed, including
all modes, generated lobby, cancel/hand-made chooser and repeated Change Map in
that same lobby. The dedicated admin/guest/late-joiner test also passed again.
Evidence: TEMP/ymca-ui-probes/callback-args-e62c30ba3a7c41adab47e663a873b135 and
TEMP/ymca-dedicated-map-probes/callback-fix-01.

Game's local CA DLL is updated; the current compatible Cameo DLL is installed too.
No user game process was stopped. The release-line fix is prepared locally;
no replacement release or server deployment has been triggered for this fix.
