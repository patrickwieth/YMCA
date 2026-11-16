# User-edited ring — authoritative runtime PNG preserved

The user replaced mods/ca/bits/china/buildings/gatling-base.png with an86x54 ring-only
PNG. supplied-ring.png is a byte-identical archived copy. Neither input pixels nor
PNG metadata are rewritten by align_supplied_gatling_ring.py. Earlier platform cuts
are historical and the old installer now refuses to overwrite this supplied ring.

The cut corresponds to the previous bearing at source translation(-21,-17). Thus the
existing mount point64,39 becomes43,22. The lower ring ellipse is centered at43,32;
Center offset0,-5 places that ground center at the unchanged Soviet1x1 bib center.
The ring top/mount is10px above ground. Turreted.Offset.Z is226 (9.99px), replacing
the old tall platform's604. This moves the whole turret/muzzle parent together.
The turret atlas, scale0.65, sequence offset0,-34, local muzzle offsets623,+/-257,834,
eight weapon profiles and barrel phase remain unchanged. Do NOT subtract the mount
translation from the local muzzle offsets a second time.

all32-native.png / rotation-native.gif were reviewed. These are native compositions,
not live captures. registration.json records source hash, mount and subpixel error.
Engine checks load the actual86x54 PNG and verify all muzzle projections against the
unchanged gun geometry. Native pixel/selection/weapon regression checks pass.
Live firing review remains pending after restart. No commit/push/release.
