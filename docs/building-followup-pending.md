# Building follow-up status

The formerly pending local building work is complete; see
[building-finish-review.md](building-finish-review.md) for implementation, native
validation and reproduction details.

- Construction stages aligned to finished PNG and legacy sprite ground anchors.
- Swiss Bank uses its builder faction's bib, including preview and remap handling.
- Allied Helipad's built cells block movement; exterior detour tested in game.
- Soviet Nuclear Plant is 4x3 with matching bib/collision and a new cameo.
- China Supply Depot is 3x3 with four external docks, two per side; traffic tested.
- Requested cameo shifts completed, including Nod Repair Depot crop [13,54,179,179].
- Regression boundaries updated explicitly and focused tests passed.

No release/push/deployment. Existing running games were not stopped. Restart via
`tools/test-game.cmd` to compile/load the current implementation.

Subsequent map work is recorded in `map-biome-followup.md`: Desert/Barren scenery,
generated Two Sides trees, the curated Handcrafted picker and the nuclear site's
shared-bib-offset correction are implemented. Glitters' reported angular edges
remain open pending a marked problem location; the general clipping experiment
was not installed.

## Current backlog (user clarification)

- **Building remap/player colors:** still open; separate from building silhouettes.
- **Existing bunker fire ports:** investigation started; window-aligned offsets
  and native firing verification are still pending. See `bunker-fire-ports.md`.
- **Urban Warfare / civilian garrisons:** a dedicated map with a city, adjacent
  villages and loose edge settlements; roads and parked civilian vehicles are also
  required. Source collection received in `mods/ca/bits/misc/buildings/` and visually
  categorized (`urban-art-inventory.md`); extraction/integration pending. See
  `urban-warfare-map.md`. Cut out and register civilian art, then connect temporary occupation,
  player-color flags and infantry firing. A weaker-than-bunker bonus is a proposal,
  not settled balance. See `urban-garrisons.md` for existing code and remaining work.
- **Glitters edges:** still open as described above.
- **Separately authorized release preparation:** including Direct Connect/Airship
  and Battlefield News; no publication authorized here.

**Not backlog:** Archipelago is accepted; long-term playtesting is explicitly not
tracked as a pending task. Scrin building differentiation/artwork is closed for now
at the user's request, not waiting for new Scrin assets. Continents and Migration
remain accepted. Speculative terrain/reveal optimization is not a concrete task.
