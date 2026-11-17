# Build Everything support-power crash

`SupportPowerInstance.GetLevel()` and `SupportPower.GetLevel()` called `Max` on an
empty prerequisites dictionary when AllTech was enabled. The UI stack surfaced
this inside SupportPowersWidget.RefreshIcons (GetLevel may be inlined).

Both now delegate to SupportPowerInfo.GetAvailableLevel: maximum eligible defined
level, default0 if none. AllTech bypasses requirements but never invents a level.
No support-power data, charge times, targeting or faction prerequisites changed.

Regression tests: SupportPowerLevelTest, four cases passed in isolated Release
output `.art-validation/support-power-fix/bin`. Full suite:146 passed, one failed:
LaunchUriBrokerTest.ForwardsUriToPrimaryInstance timed out (separate IPC test).

Installation attempt into engine/bin was blocked by an in-use Common DLL. No game
process was stopped. Close the game normally and run test-gebaeude.cmd, which builds
current source before launching. The isolated tested DLL is not yet the live DLL.

## Internet Center prerequisites checked

Resolved STEK and chpropaganda both provide `tier3` without an additional condition.
Internet Center requires `tier3, ~structures.china`, has Building queue and limit1,
and provides tier4. A Soviet tech center alone does not grant Chinese construction
permission. No speculative tech-tree/balance change was made.
