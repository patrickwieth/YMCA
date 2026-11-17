# Plains: recognizable starting resource patches

Scope: the **Plains preset** (serialized `Arabia`) and Custom Plains **Flat / Rocky**, with **Patches**. Water Basins / Custom Ponds retain their separate resource placement; this is not a global resource-policy change.

## Placement

- Preserve the radius-10 generator-grid build clearance.
- Keep the existing starting-field radii 5–7 (81–149 resource cells). Put the center at radius + 11 through radius + 13 from its start instead of the former 18–24 range. Integer rounding is checked in regression tests: the nearest cell is outside radius 10 and within radius 14.
- Every cell of the starting deposit must be at least three native ground-cell units closer to its own start than any other symmetry slot. Distance uses the isometric metric `sqrt(2*dx*dx + 0.5*dy*dy)`, not an unweighted generator-grid distance.
- Reserve a radius-two direct land approach before rocks are placed. Tech-building anchors cannot occupy this approach. Final generator validation checks the reserved approach remains traversable.
- Retain the second group's radii 4–6 and center distance 28–37. Its centers must stay at least radius + 16 generator units away from all starts. Maintain separation between deposits, including between the two groups.
- Prefer the existing common rotated placement. If proximity or objective constraints prevent it, plan complete equal-sized deposits independently within the same distance bands. Commit only after every slot has a valid placement. No partial fields, dropped deposits or relaxed ownership checks. Exact rotational symmetry is therefore not guaranteed for resource locations; group sizes/counts remain equal.

## Strategic protection

Tests exposed the legacy objective post-pass trimming starting deposits. For Plains/Patches, objective footprints are now reserved before economy and mountains. The front uses the existing team-axis-weighted native path search, avoiding arbitrary equal-length map-edge detours. Checkpoint pads, not a resource-free full corridor, are reserved for this open-map preset. Rock feet cannot consume them. Complete-front reachability and ordering are validated; invalid layouts retry.

The optional corridor parameter on `ReserveFront` defaults to the previous behavior for other callers. Mountain Valleys and Two Sides keep their corridor reservations.

## Evidence and limits

`%TEMP%/ymca-plains-start-fields`:

- `native-final.log`: **72 passing plans**, Flat/Rocky × Tactical/Operational/Strategic × 2/3/4/8/12/16 players × seeds 42/43. Independent assertions check near-field distance, native ownership, reachability and full disconnected field sizes after the complete generation pipeline.
- Final Flat/Rocky 4P42 exports and a finite Strategic Rocky 12P43 export passed; the default Rocky canonical comparison passed.
- Actual client terrain view captured for Rocky 4P42. This is not a fresh harvester delivery/income test.
- Ponds 16P43 Patches and Rocky 4P42 None plan controls passed. An exploratory broader Ponds change was rejected and scoped out, preserving that preset's existing placement.
- Rocky 4P42 Glitter failed generation on **both the prior installed binary and the modified staged binary**. This preexisting dense-economy/rock-placement issue is separate and not repaired here. Do not describe all resource modes as certified.
- Modular tests: 180 passed. No all-seed, balance, multiplayer, editor lifecycle or performance certification.

Earlier diagnostic matrix logs include rejected iterations; only `native-final.log` represents the final native-distance implementation.

Existing map packages remain unchanged. Restart after installation and regenerate the map.
