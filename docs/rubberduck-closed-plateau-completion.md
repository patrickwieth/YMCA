# Required closed plateaus: completion of the generator/editor migration

This supersedes the partial rollout in `rubberduck-closed-generator-planning.md`.
**New Mountain Valleys plateaus no longer silently fall back to legacy cliffs.**
Existing maps remain compatible; no saved package is automatically rewritten.

## Fixes

- Corner validation now checks the actual wall-owning cells. The former radius-one
  ramp exclusion rejected valid inward corners with a diagonal interior ramp.
  Both wall owners must be flat height four, and an interior ramp must meet the
  shared height-four vertex. Ramp-owned or mismatched-height sockets still reject.
  Positive and negative cases are tested in all four orientations.
- Small islands additionally try a ramp-oriented rectangle with a six-row corridor.
  It must be entirely inside the original footprint and retain every interior cell;
  only fringe cells may be removed. Centered rotation/reflection-safe roof bands
  and the original outline remain alternative candidates.
- Every candidate must reserve a complete valid foot ring. Failed homes cause a
  layout retry; failed optional islands cause another symmetry-orbit proposal.
  There is no branch that commits an unsupported legacy plateau.
- Island orbits are drafted on an independent `MapPlan.Clone`. All members commit
  together, or the entire draft is discarded. Previously reachable ground outside
  the newly blocked footprint must remain reachable. Attempts are bounded.
- New plans set `RequireClosedPlateaus`. Export checks full selection coverage and
  fails before writing a map if economy or later planning invalidated a contour.
- The generator no longer runs the obsolete legacy apron expansion after committing
  complete feet. Normal editor replanning retains its compatibility implementation.
- Matrix export reuse recognizes the new requirement: terrain-only equality cannot
  reuse an old actor graph.

## Final checks

Evidence root: `%TEMP%/ymca-closed-finish`.

- `matrix`: **72 actual exports**, all player counts 2 through 12 plus 16, Tactical /
  Operational / Strategic, seeds 42 and 43. All **866 filled plateaus** have complete
  closed reservations. None of these maps contains a legacy native-cliff actor.
- `--rubberduck-closed-contour-test MAP_DIRECTORY` reloads these exports and audits
  every closed actor against the current socket/vertex rules. All 72 passed after
  the final shared-vertex check was added.
- `accepted-normal` and `accepted-shaded`: actual clients on the production 4-player
  seed-42 terrain, eight plateaus and **640 closed actors**. Each run passed 32
  ascent/descent legs, 16 blocked-foot checks and MCV deployment.
- `accepted-editor`: actual ordinary-editor extension, nonmutating draft, exact
  Undo/Redo and save/reload: five PASS categories. The saved result retains all
  640 closed actors and contains no legacy native-cliff actor.
- Twelve generation checks: 4/16 players, None/Glitter/Patches, finite on/off.
- Eight other presets at 4 players / seed 42 still match the previous production
  `map.bin` and `map.yaml` byte-for-byte. Their established terrain identities,
  collision, water links and economy are not forcibly converted into plateaus.
- Contour tests retain the 180 valid / 543 invalid cases, five original fixtures,
  protection and composition checks, plus rotation/reflection, draft-clone isolation,
  small-island rectangle fitting, and strict ramp-corner ownership tests.
- Release build passes. A build attempted alongside utilities initially hit a Windows
  DLL lock; it was rerun successfully after those processes ended.

## Scope, not hidden fallbacks

The separate **nonwalkable mountain-barrier renderer** and height-zero rock coasts
are different terrain systems. The subsequent [mountain material update](rubberduck-mountain-materials.md)
now replaces the mountain wall's repeated texture strip with a projected authored
rock material, without changing collision or wall geometry. They are not unfinished substitutes inside a newly
committed walkable plateau. This change does not widen those barriers, unblock them,
cover their blocked dirt with grass, or relabel them as walkable high ground.
Their broader visual acceptance remains separate, as do combat elevation and
large-map frame-pacing certification. No claim of complete visual acceptance of
all terrain systems is made here.

Bridges remain deferred until the supplied sprites arrive. Source sheets and
installed cliff PNGs were not modified by this planning change.
