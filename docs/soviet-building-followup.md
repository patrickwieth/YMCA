# Soviet/China building placement and cameo follow-up

Local Game worktree; no release, push or deployment.

## Russia Tesla Reactor

The first inspection of `soviet/structures.yaml` suggested a 4x4 site, but that
was not the runtime result. `building-ground-masks.yaml` overrides TPWR to a 3x3
site with `xx= xx= ===`: four occupied cells and five passable cells. The resolved
actor still had the old 4x4 rectangular HitShape and a positive center offset
`0,724,0`, placing its foundation and body below their proper cells.

The fix preserves the **actual existing footprint and occupied/passable cells**:

- Body center: `0,-724,0`, centered on the occupied rear 2x2.
- Collision rectangle: `-1024,-1024` to `1024,1024`, matching that body.
- Soviet 3x3 bib retained. Body-local offset `-1134,0,0` transforms through the
  engine's BodyOrientation to world `0,724,0`, cancelling the body offset and
  keeping the foundation centered on the 3x3 placement site.
- Body source offset: `1,9` for idle/damaged, centering the unchanged artwork's
  reviewed ground contacts on the occupied body. Scale 1, original PNG unchanged.
- Selection ground bounds: `2896,2896,0,0`, matching the 2x2 body. Height unchanged.

No cost, health, power, overload behavior, prerequisites or source artwork changes.
Collision position/extent is intentionally corrected; this is not described as a
purely cosmetic change. The intermediate legacy bib-occupancy/grid files are still
overridden by the final ground-mask layer; the resolved engine check, not the raw
first declaration, is authoritative. Nuclear Powerplant and other actors untouched.
The legacy Soviet calibration script refuses to overwrite this reviewed registration.

`--check-tesla-reactor-alignment` passed against the engine-resolved rules, including
an actual BodyOrientation transformation, all nine footprint cells, collision and
selection. The isolated build passed. `tesla-native-aligned.png` shows the selected
building aligned to its foundation. `tpwr-before.yaml` / `tpwr-after.yaml` preserve
the resolved diagnostic data.

## Requested cameos

The source crop window moved down, making the artwork sit higher and exposing more
facade rather than mostly roof:

- Soviet Tech Center: `(15,44,168,158)`.
- Kennel: `(29,41,103,96)`.
- Industrial Plant: `(31,67,231,217)`.
- China Command Center: `(24,58,232,214)`.

China Radar Upgrade initially used a bounded dish cut `(31,9,57,48)` from the existing
`bits/china/buildings/radar.png`, the source used by `chcommand.supplied/radar`.
The subsequent request for both dishes and a matching placement ghost is implemented
in `building-ui-followup.md`, superseding that single-dish crop.
This is a source cut, not an invented high-resolution building. The small native
source limits detail. It replaces only the upgrade's icon, not its plug behavior
or the actual command-center radar sequence. No new screenshot-derived source.

Only those four existing cameo files changed; `chcommand-radar.png` is new. Other
cameos, approved power icons, Soviet Barracks/Nuclear Powerplant, and all supplied
building PNGs remain unchanged. Catalog/source bindings and provenance record the
changes; `prepare_faction_building_icons.py` reproduces them.

Evidence: `art-source/soviet-building-followup/`, including before/after crop sheet,
complete Soviet/China native galleries, native result JSON and focused regression
results. The gallery fixture includes duplicate original/diagnostic actors; these
are not duplicate production entries in the real game.
