# Supplied Soviet nuclear plant

Local implementation, 2026-11-25; no release/deployment.

Source `nuclear_power_plant-1114443554-0.png` is archived unchanged in
`art-source/soviet-nuclear-plant/original.png`. The reproducible cutout preserves the
source RGB and 256×256 canvas of that initial pass. The user subsequently refined
the authoritative runtime PNG to 280×256; do not rerun the old cutout over it. Main buildings, both cooling towers, overhead piping
and the attached fenced substation remain. Trees, foreground troops/vehicles and
loose courtyard remnants are excluded. No geometry redesign or procedural texture
replacement is used.

`APWR.Soviet` / `apwr.soviet` idle and damaged-idle now resolve directly to
`mods/ca/bits/soviet/buildings/nuclear-power-plant.png`. The former spaced-filename
PNG is preserved. Registration: Center anchor, scale1.0 (unscaled, per user request), offset3,-53 after the final site-registration correction (superseding the earlier 10,-76 placement). Previously scale1.4; only the display scale was changed, not the PNG or gameplay. Both later
placement layers carry the same values, rather than silently overriding the new
placement. The later approved 4×3 site now uses the matching Soviet 4×3 bib and 5792×4344
historical selection dimensions. The final correction now derives the selection
from `DecorationFootprint: 4,3`, rather than a screen-axis diamond; tower selection
height remains 110. Its nuclear-only bib offset cancels the shared service-depot
sequence's 32,-16px shift. See `map-biome-followup.md`.
The legacy scale1/Bottom-anchor calibration tool explicitly refuses to overwrite
this reviewed registration.

The user subsequently requested the 4×3 footprint/foundation and matching collision
rectangle. Costs, output, health, prerequisites and nuclear death weapon remain
unchanged. The supplied single frame is used for both healthy and damaged bodies.
The new Nuclear Plant cameo uses that PNG directly. Existing construction stages
are retained and now aligned to the finished body's ground anchor; see
`building-finish-review.md` for the final native checks.

Native healthy/damaged and selected-detail views were inspected. Engine-resolved
filename/geometry and screenshots are in `art-source/soviet-nuclear-plant/validation/`.
`tools/prepare_soviet_nuclear_probe.py` creates isolated review packages, optionally
`--selected` with `YMCA_BUILDING_REVIEW_PROBE=1`. Never install diagnostic maps.
Restart to reload artwork; map regeneration is not needed for this actor replacement.
