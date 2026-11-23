# Fixed double-elimination routing — local, unreleased

## Scope and compatibility

New double-elimination tournaments receive BracketRoutingVersion=1 when started. Their shuffled Entrants order is then the fixed seed order; series retain a BracketSlotId. Historical/running records missing the version default to 0 and continue using the original loss-group scheduler. No historical series, results, replay URLs or map downloads are rewritten. No live state was accessed or changed for this work.

Version 1 builds a deterministic bracket DAG. Winners advance through W slots. First-round losers enter L1; subsequent upper-bracket drops cross into the opposite surviving lower lane before those lanes consolidate. This postpones early return matches rather than dynamically re-pairing by participant order. It cannot eliminate all later rematches, especially in small fields.

Fields are padded to a power of two with distributed byes. Byes resolve as empty inputs, not invented matches/wins/losses. Grand final F1 pairs upper and lower winners; F2 is created only if the previously unbeaten finalist loses F1. Series walkovers use ForfeitWinnerDiscordId without fabricating games. Existing global round barriers, map freezing, best-of handling, explicit deadline arming and result processing remain in place.

## Related local fixes

- The Python match-safety guard normalizes .NET seven-digit fractional timestamps for Python 3.9. Any discarded precision rounds upward, never shortening host retention. Invalid/timezone-less values still fail closed. This has not been executed against or installed on the controller.
- The pending bracket UI now marks walkover winners and winner/loser origin links correctly without adding game scores or replay links.
- Previously prepared UI and exact-name resolution changes remain local; no release/deployment is implied.

## Validation

- Simulated brackets for every size 2–64, multiple outcome patterns, BO3 and series walkovers: no duplicate simultaneous entrant, no eliminated entrant scheduled, correct upper/lower loss counts, two-loss elimination, bounded match totals, grand-final reset and JSON restart stability.
- Explicit opposite-half feed assertions for an eight-player bracket.
- Coordinator integration for 3/8/9 entrants, including actual state-file reloads between rounds and a forced grand-final reset. Separate legacy-running eight-player case retains version 0 and empty slot IDs throughout.
- Full TournamentBot.Tests suite; Python safety tests including fractional-second boundary and timezone offset cases.
- Browser checks against a freshly rendered synthetic preview: desktop/mobile, orthogonal edges, hover paths, details/replays, keyboard/source navigation, refresh state/focus/scroll and no-JavaScript fallback. Browser uses isolated headless Edge, not the user's browser session.

The live tournament remains untouched. Before an approved deployment, inventory the controller revision/state, rerun match safety there, and verify historical download routes afterward. M0088's disconnect cause still requires client logs; this change does not diagnose it.
