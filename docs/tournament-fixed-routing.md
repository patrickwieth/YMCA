# Fixed double-elimination routing — deployed 2026-11-23

Authorized bot-only rollout **45249852873** succeeded at 19:32 UTC, source `61f9a49aeeb58ab04b90aa5998f981260440f5da`. Discord registration, binary identity, unchanged configuration and historical state were verified. All 62 T013 replay downloads and 62 map downloads retain their pre-rollout hashes. Live desktop/mobile browser checks passed. Follow-up read-only run **45250143135** succeeded. The installed game image is unchanged; only bot binaries were replaced.

## Scope and compatibility

New double-elimination tournaments receive BracketRoutingVersion=1 when started. Their shuffled Entrants order is then the fixed seed order; series retain a BracketSlotId. Historical/running records missing the version default to 0 and continue using the original loss-group scheduler. No historical series, results, replay URLs or map downloads are rewritten. Development used synthetic state. Deployment compared the quiescent historical tournament/series/match data before and after; all remain unchanged.

Version 1 builds a deterministic bracket DAG. Winners advance through W slots. First-round losers enter L1; subsequent upper-bracket drops cross into the opposite surviving lower lane before those lanes consolidate. This postpones early return matches rather than dynamically re-pairing by participant order. It cannot eliminate all later rematches, especially in small fields.

Fields are padded to a power of two with distributed byes. Byes resolve as empty inputs, not invented matches/wins/losses. Grand final F1 pairs upper and lower winners; F2 is created only if the previously unbeaten finalist loses F1. Series walkovers use ForfeitWinnerDiscordId without fabricating games. Existing global round barriers, map freezing, best-of handling, explicit deadline arming and result processing remain in place.

## Related local fixes

- The Python match-safety guard normalizes .NET seven-digit fractional timestamps for Python 3.9. Any discarded precision rounds upward, never shortening host retention. Invalid/timezone-less values still fail closed. This was executed on and installed on the controller during the guarded rollout.
- The pending bracket UI now marks walkover winners and winner/loser origin links correctly without adding game scores or replay links.
- The prepared UI and exact-name resolution changes were included in the authorized bot-only rollout.

## Validation

- Simulated brackets for every size 2–64, multiple outcome patterns, BO3 and series walkovers: no duplicate simultaneous entrant, no eliminated entrant scheduled, correct upper/lower loss counts, two-loss elimination, bounded match totals, grand-final reset and JSON restart stability.
- Explicit opposite-half feed assertions for an eight-player bracket.
- Coordinator integration for 3/8/9 entrants, including actual state-file reloads between rounds and a forced grand-final reset. Separate legacy-running eight-player case retains version 0 and empty slot IDs throughout.
- Full TournamentBot.Tests suite; Python safety tests including fractional-second boundary and timezone offset cases.
- Browser checks against a freshly rendered synthetic preview: desktop/mobile, orthogonal edges, hover paths, details/replays, keyboard/source navigation, refresh state/focus/scroll and no-JavaScript fallback. Browser uses isolated headless Edge, not the user's browser session.

Future deployments must again inventory the controller revision/state, rerun match safety and verify historical download routes. Private rollback data is under `deploy/data/recovery/bot-only-20261123T193146Z-61f9a49aeeb5/`; never publish it. M0088's disconnect cause still requires client logs; this change does not diagnose it.
