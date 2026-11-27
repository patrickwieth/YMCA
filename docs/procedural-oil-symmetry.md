# Procedural Two Sides oil symmetry

Built and installed locally in Game after checking that no user game was running. Not part of the tournament bot-only deployment or a game release.

## Defects fixed

Dense Two Sides previously validated each mirrored oil independently, and could accept only one half (or overwrite an existing anchor on one side). Sparse oil also checked only a single anchor tile. In addition, reflecting the anchor did not reflect the actual 2x2 building center.

`SymmetricOilPlacement` now reflects native CPos footprint centers, validates both full footprints and their approaches, and commits both actors atomically. It rejects water, non-land, plateau surfaces, protected strategic areas, starting build clearances, entrances/bottlenecks, resource generators, overlapping oils and resources under the footprint. Dense clusters try additional candidates rather than leaving an unmatched oil. Their search radius is five planning cells to accommodate non-overlapping footprints; target remains 4–6 mirrored pairs per cluster site.

The protected-layout pipeline checks again after strategic objectives/coast validation. If an objective invalidates symmetry or footprint placement, it retries the layout rather than deleting one actor or altering terrain to force a fit.

This affects only newly generated procedural Two Sides maps. Existing archived maps/UIDs and handcrafted Two Sides are not repacked or edited. Regenerating a seed with the new code may produce different oil placement; archived packages remain authoritative for historical matches.

## Validation

`--symmetric-oil-test` generates 54 complete maps: 2/4/8 players, Tactical/Operational/Strategic, None/Sparse/Dense oil, and two seeds. Independent native-coordinate center-sum assertions check every counterpart; dry-bank flood fills check equal oil counts on both sides; occupied 2x2 cell sets check overlaps, terrain, resource fields and strategic reservations. Exported actor coordinates were also checked independently against the actual map bounds. Focused tests reject an invalid mirror without partially adding an actor, reject repeated placement, and reject water under a footprint whose anchor is clear.

A native exported 2-player Strategic dense map (seed 42) was run in the engine. All ten oil derricks were reachable by engineers from both starts. Engineer, jeep, medium tank and destroyer actually traversed their test routes; all fourteen existing construction-yard types retained legal placement near both starts. The overview was inspected. The review-only probe package is not installed in normal map folders.

Evidence: `art-source/handcrafted/validation/procedural-two-sides-oil-runtime.json`, `procedural-two-sides-oil.png`; installed-build generator matrix log: `procedural-two-sides-oil-matrix.txt` in the same validation directory.

## Still separate

Plains and Mountain Valleys have now received a separate owned-economy/access policy; see `start-economy-balance.md`. It balances per-start budgets and routes rather than promising exact rotational placement in irregular valleys. Other presets, projectile obstruction and visual faction/color issues remain separate. The previously approved handcrafted coast/tree shapes are untouched.
