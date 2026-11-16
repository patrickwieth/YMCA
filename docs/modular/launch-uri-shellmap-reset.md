# Forwarded launch URI: stale lobby slots on shellmap transition

Reported failure: CreateMapPlayers looks up `Multi1` while loading a shellmap from
Game.ProcessPendingLaunchUri. The shellmap does not define the old match's slots.

Root cause: both forwarded replay and server-link branches called LoadShellMap
without first disconnecting/resetting the old OrderManager and lobby UI. Normal
quit-to-menu already performs Disconnect, Ui.ResetAll, LoadShellMap in that order.

Fix: both URI branches now call ResetSessionForLaunchUri (Disconnect + Ui.ResetAll)
before LoadShellMap. The normal player-reference validation is retained; unknown
match slots are not silently skipped. A fresh local spectator-only EchoConnection
session replaces old match/replay state.

Regression: LaunchUriSessionResetTest injects an occupied Multi1 lobby and executes
the actual reset helper, verifying a new local manager with no slots and one
slotless local spectator. This test plus the four support-power crash tests pass
in isolated Release output `.art-validation/launch-uri-reset/bin`.

Runtime installation was attempted but both OpenRA.Game.dll and
OpenRA.Mods.Common.dll in engine/bin are locked. No processes stopped. Close all
running instances using this worktree normally, then run test-gebaeude.cmd to
rebuild and launch. Live forwarded-link switching remains untested.
