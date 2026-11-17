# Supplied Soviet kennel integration

`mods/ca/bits/soviet/buildings/kennel.png` is directly bound to KENN through
`kenn.soviet`, without recoloring/resizing or replacing the original.
SHA256:1ef7658d72edf495de81db6f858324a85ae81e2ad0987b7c17cda1695c263568.

Native128×128, Scale1, Bottom offset-1,17. Estimated ground center65,79 aligns
with the1×1 Soviet decorative bib; the bowl remains on the front of the foundation.
Selection decoration905×905, height70. Legacy miniature SHP bib removed. Idle and
damaged-idle use the supplied PNG (no separate damaged artwork was supplied).
China construction remains inherited. No changes to1×1 gameplay footprint,
production, prerequisites, dog exits/spawn offsets, cost, HP or power.

`placement-preview.png` is an offline nearest-pixel placement review, not a game
screenshot. `--check-soviet-kennel-art` loads the native image and resolved sprite/
construction states plus dependencies. Regression tests verify source bytes and
unchanged production/exits/gameplay against `rules-before.yaml`.
The kennel already exists on testmap1; original map untouched. Live dog production/
exit alignment review remains pending. No release/push performed.
