# Fixed random map sequences per tournament round

`/tournament-create` now accepts optional `map-selection`:
`player-choice` (default) or `round-random`. Existing records deserialize to
PlayerChoice and retain existing behavior. Mode appears in the registration
announcement and tournament status; random rounds announce the ordered maps.

RoundRandom freezes BO1/BO3/BO5 sequences once, inside the serialized state
transition that schedules a round. The whole sequence is persisted in
`TournamentRecord.MapsByRound` before any match is enqueued. Every series in the
same round, including a concurrent third-place playoff, receives those exact
map requests in the same order. Generated seeds/IDs are not rerolled per pairing,
restart, later game or rematch. Later rounds freeze their own new sequences.
`RandomMapByRound` remains available for old/player-choice tournaments and stores
the deciding slot for random rounds.

Random mode queues game one immediately and skips pick DMs; stale/manual pick
submissions are rejected. Series advancement still queues only required games.
The tournament's snapshotted pool is used, not a later edited global pool. Small
pools cycle, and adjacent repeats are avoided when multiple types are available.
The previous-type comparison uses the pool UID, not a generated request UID.

Validation: 66 ordinary bot tests passed. New cases cover generated/fixed maps,
BO1/BO3/BO5, 1v1/2v2, single/double elimination, shared seeds and persisted order,
subsequent rounds and third-place series, rematches and second-game progression,
early series completion, one-/two-type pools, old JSON defaults and rejection of
invalid modes/player picks. The opt-in native host tests are not rerun for this
bot scheduling/UI change. Actual Discord user interactions are not automated.

Bot-only update; game clients remain v0.97.4. Source commit `8270cfa7` was
successfully deployed with the normal match safety guard, without cancelling or
resetting tournaments: https://github.com/patrickwieth/YMCA/actions/runs/44474533553 .
Discord login/command registration and host v0.97.4 were verified on November 19,
2026 at 06:48 UTC. No client release or public game-server deployment was needed.
