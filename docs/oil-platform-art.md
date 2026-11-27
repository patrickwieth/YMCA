# Supplied oil platform replacement

Locally installed, 2026-11-25. No release/deployment.

Source: `soviet_oil_platform_...-358960086-0.png`, archived unchanged as
`art-source/oil-platform/original.png`. `tools/prepare_oil_platform_art.py` preserves
the 256×256 canvas and upper-structure RGB, using bounded component masks and removal of the
source's disjoint cool sky/water colors. Sky, sea, foam, birds and distant boats are
removed, including the background between derrick/crane struts. Structural pipes,
stacks, cables, piers and muted highlights are retained.

The user rejected the initial foam-eroded pier ends. The three lower ends now have
closed cylindrical contours without alpha holes or isolated teeth. Only bounded
lower-pier regions change: missing material is taken from clean rows of the same
source column and closed with a muted source-color rim. These previously submerged
ends are explicitly inferred, not recovered hidden source pixels. The upper platform
is byte-identical to the initial cutout; that earlier cut remains archived as
`cutout-before-pier-repair.png`. Native X/Y bridge-context views were re-inspected.
Regression: `tools/test_supplied_industrial_art.py`.

Follow-up water contact: the user requested water around the pier feet like the
naval yards. `tools/prepare_oil_platform_water.py` now extracts three small foam/ripple
cuts from the original source and registers them to the repaired ends. The separate
`oil-platform-water.png` decoration has the same canvas, scale and offset as the
body, with soft translucent edges and only a narrow lower-face overlap. It adds no
sea-colored rectangle and does not modify the repaired structure or its shadow.
The water contact is static source artwork; no wave animation is claimed. An initial
stronger foam version was narrowed and softened after native inspection. Final
installed-runtime detail screenshot: `validation/water-contact-final.png`.
The decoration was hidden while construction was incomplete or the actor was warped.

Superseded by user request: the separate water-contact overlay and sequence are now
removed. Its PNG is preserved only as `art-source/oil-platform/water-contact-retired.png`;
the reproducer writes there, not into runtime assets. The building-test checker no
longer requires the retired sequence. The user will add the waterline themselves.
`oil-platform.png` was not rewritten. Native confirmation is archived as
`validation/water-contact-removed.png`.

Runtime file: `mods/ca/bits/terrain/environment/oil-platform.png`.
`oil_platform` idle, damaged-idle and make sequences reference it directly, at scale
2.5 and offset0,-55. The old detached crane/wave overlays are removed. The supplied
single-frame artwork has static cranes and no separately invented damaged painting;
existing damage effects remain. The original 36-frame make duration is retained by
repeating the new image. Existing construction effects and sidebar icon are retained.

The 4×4 water footprint, cost, health, power, income, efficiency radius, prerequisites
and build-time rules are unchanged. Native screenshots were reviewed beside both
bridge orientations; evidence is shared with
`art-source/bridge-repair-hut/validation/`. Restart to reload the replacement artwork;
existing maps do not need regeneration just for the oil platform.

Building-test placement: `tools/test-game.cmd` now adds `TestOilPlatform` at native
cell `164,-64`, to the right of the Soviet display block, owned by Multi0. The
water-preparation command creates a dedicated 4×4 water footprint with a one-cell
border. The checker loads the body/water-contact sprites and rejects out-of-bounds
or overlapping pool placement. Existing actors and the immutable `testmap1.oramap`
source remain unchanged; the addition is made idempotently in the disposable copy.
Native grid-map review and `test_extra_faction_buildings.py` passed.
