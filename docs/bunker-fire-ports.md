# Existing bunker fire ports — in progress

User requested moving the existing bunkers' firing positions approximately to
their visible windows/slits. Investigation started, then the user introduced the
Urban Warfare planning task. **No production fire-port offsets have been changed
or validated yet.** Do not report this request as complete.

Confirmed stationary garrison families:

- chbunker, chbunker.advinf, chbunker.ai: `chbunker.supplied`, supplied China bunker
  PNG 256x256, Scale1, Bottom anchor, Offset0,12.
- chbunker2, chbunker2.advinf: `chbunker2.supplied`, fortified China bunker PNG
  256x256, Scale1, Bottom anchor, Offset0,21.
- NKOREA_BUNKER: `nkorea_bunker`, Soviet bunker PNG235x180, Scale1, Bottom anchor,
  Offset4,-21. Resolved building center differs from its early definition; use
  final rules (LocalCenterOffset0,-1448,0 and FootprintOffset-1,-1).

All currently inherit the generic six ground-level `^AttackingCargo.PortOffsets`.
`AttackOpenTopped` selects a port by passenger index and transforms it through
BodyOrientation before moving the passenger's firing position. Existing infantry
armament muzzle offsets must also be accounted for; changing the cargo anchor alone
is not proof of the final projectile emission position.

Source images were inspected and enlarged grid reviews created under
`.tmp/bunker-{china,fortified,korea}-grid.png`. The resolved icon/sequence snapshot
is `.tmp/bunker-fire-before.json` and `.sequences.json`; actor dumps are
`.tmp/bunker-fire-{chbunker,chbunker2,NKOREA_BUNKER}.yaml`.

Next: select visible slit/window coordinates on each image, convert using resolved
sprite placement/body transforms, configure per-family offsets (not the shared
transport defaults), and inspect real native infantry shots/muzzle flashes.
Preserve capacities, weapons, bonuses, footprints and source art. Mobile bunker
vehicles/Helix transports must not inherit stationary building coordinates.
Allied PBOX/HBOX use fixed weapons rather than this garrison path; their muzzle
registration is a separate case, not covered by generic cargo-port edits.
