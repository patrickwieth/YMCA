# Authorized championship reset — 2026-11-20

User authorized v0.97.6 update with interruption, then explicitly requested regenerating the championship with the same participants and sending everyone fresh invitations because of stale/confusing invitations and password reports.

- Update run 44808156720 succeeded; live M0034 handshake verified v0.97.6. Backup: `data/recovery/pause0976-20261120T200532Z/`.
- M0032/M0033 belonged to completed T011 and were incorrectly marked WaitingForPlayers before the update; startup resumed them. Do not confuse them with T012 pairings.
- Reset run 44811109584 prepared T013 and restarted the bot; invitation phase initially failed due to camel-case config keys. Follow-up 44811236298 delivered all 16 invitations successfully, recording Discord message receipts privately on host.
- Backup: `data/recovery/reset-T012-20261120/{before-stop.json,quiescent.json}`. No backup state or credentials committed.
- T012 cancelled; its series closed against stale map-selection interactions. M0032–M0037 cancelled and credentials cleared; historical completed tournaments otherwise preserved.
- T013 retains the same 16 entrants and format (double elimination, BO3, generated maps, player choice). Fresh shuffled first-round series S0019–S0026. Existing round-one deciding map retained; other maps selected anew by players. No results carried into new tournament.
- All 16 received explicit cancellation/reset notice, new opponent, v0.97.6 requirement, working map-selection components and new bracket URL. Game join DMs follow both players' picks, as before. User LördenMcFairGenü now faces Hiraku, S0021.
- Before reset, actual process command passwords matched persisted passwords for M0034–M0037, with two recorded join DMs each. This does NOT establish client-side join success or fully diagnose reported password errors. Do not claim a password-code fix was deployed.
- Existing unrelated local cleanup changes in Game-release were NOT deployed or modified. Permanent lifecycle cleanup remains separate work.
- Maintenance workflow restored to read-only verification after operation; normal release/bot workflow untouched. One-off script lives only on maintenance branch.
