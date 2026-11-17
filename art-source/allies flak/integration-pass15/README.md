# Approved Allied flak pass15 — local AGUN installation

Approved base and32-direction turret copied byte-for-byte to
`mods/ca/bits/allies/buildings/flak-{base,turret32}.png`. Source visible pedestal
pixels remain exact; the small originally obscured top region was reconstructed
and documented during pass14. No further art/model changes during installation.

AGUN now uses `agun.native`, independent fixed body/rotating turret, no legacy bib.
Native256 frames, Scale1; base Center0,-34, turret Center0,-5, attachment0,0,656.
Original construction/sell/icon inherited. Damaged states reuse approved art.

Explicit visual muzzle changes: original two slots520,100,450 /520,-150,450 now
1243,195,1287 /1058,-195,1500, corresponding to opposite physical barrel mouths.
ZSU-23, fire rate, slot count, health, cost, power, prerequisites and turning remain
unchanged. Engine-native loader, trait construction and32-facing muzzle check pass
(max0.172px). Baseline/after resolved rules stored here; regression compares gameplay.

CRAM inherits AGUN. Its obsolete `-WithBuildingBib` and
`-WithEmbeddedTurretSpriteBody` removals became redundant and were removed. CRAM's
approved pass11 assets, effective traits and muzzle calibration remain unchanged,
verified by its existing regression and native utility check.

Runtime DLL builds successfully. Live firing/appearance not yet observed. The
separate **Flak Voxel Recoil Test (local art)** map includes the existing AGUN
position with this new art, without changing the original testmap1 archive.
