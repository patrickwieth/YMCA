# Supplied Nod Sub Pen — installed locally

Directly binds `mods/ca/bits/nod/buildings/submarine pen.png` to SPEN.nod's existing
spennod namespace. Native300x256, Scale1, Bottom anchor, offset0,0. Idle, damaged
idle and placement use the supplied image; there is no separately supplied damaged
art. Original PNG untouched, SHA256
261ce2d16bece3880dd3189ffc04358270704f3755abcfeb437ca845a8130ac5.

Construction animation/icon remain inherited. Selection decoration height raised
from30 to160 to cover the tall building. All other resolved actor traits match
rules-before.yaml, including cost, HP, power,3x3 footprint, water placement, three
exits, production, repair and underwater detection. No new bib or painted water.

The existing normal test-map water helper now includes SPEN.nod along with Allied
and GDI Naval Yards, including a one-cell border for exits. Regenerated the normal
disposable testmap1-debug.oramap and successfully applied three water pools/75 cells.
The dedicated flak-map package is not accepted by that helper's safety guard; no
water change is claimed there. Original testmap1.oramap hash unchanged.

Runtime build succeeded, --check-nod-sub-pen-art loads actual native PNG states,
construction/icon and actor dependencies; three Python regression tests pass.
Live appearance, selection and submarine drive-out review remain pending.
No commit/push/deployment. Restart test-gebaeude.cmd for the current rules/map.
