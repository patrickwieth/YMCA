# Supplied Soviet helipad — local native binding

`mods/ca/bits/soviet/buildings/helipad.png` is used directly, without editing,
resizing or color-matching the supplied194x135 PNG. `hpad.soviet` overrides idle,
damaged-idle, active and damaged-active with single-frame Scale1/Bottom states.
Construction/sell/icon remain inherited. Ground contacts recorded in
`calibration.json`; offset -1,2 and decoration1901,1901,0,11 / height62 align the
native platform. `native-placement.png` shows the retained decorative3x3 bib and
ground-selection guide; it is not a live screenshot.

Existing2x3 footprint, hit shape, spawn/docking, repairs, production, cost, HP,
power and prerequisites unchanged. GDI HPAD resolved rules remain identical.
Before/after rules and resolved sequence filenames stored here; integration tests
verify all four direct filenames and preserved gameplay. Supplied image hash is
recorded in calibration.json. Live helicopter landing/reload is not yet observed.

The separate **Flak Voxel Recoil Test (local art)** map replaces its old HPAD
position with HPAD.Soviet for inspection; original testmap1 archive stays untouched.
