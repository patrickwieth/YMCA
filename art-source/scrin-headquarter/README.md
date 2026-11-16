# Scrin Headquarter — supplied artwork and small vein-end finish

`headquarter-original.png` preserves the supplied file unchanged. The explicitly
requested finish adds short tapered ends to nine truncated lower/peripheral vein
strokes. No architecture, glow, machinery, or upper silhouette was redrawn.
Source-like dark wine/mauve tones and narrow highlights taper into the terminals;
132 pixels changed. `finish.json` records original/final hashes and terminal positions.
`before-after-3x.png` is the visual comparison, not a live screenshot.

Reviewed `headquarter-finished.png` is promoted to the authoritative runtime path
`mods/ca/bits/scrin/buildings/headquarter.png` by explicit user request. It stays RGBA;
no indexed Scrin palette is applied. The other eight supplied Scrin images are indexed
and now each use their own embedded palette instead of legacy player palettes.

Bound to SFAC (Construction Yard) and its SFAC.Nukular variant. Building-placed and
pdox states also use the new art, avoiding flashes of the old SHP. Existing production,
MCV transform targets, prerequisites, footprint, health and costs are unchanged.
An existing duplicate Inherits key on SFAC.Nukular was given the unique @NUKULAR
suffix so the later image override can merge; resolved gameplay remains identical.

Scale1.25, Bottom offset0,-27, selection3620×3620/height201. Source/placement,
variant preservation and engine loading checks pass. Live placement/production
review still pending. No process stopped, no commit, push or release.

Reproduce finish with `tools/art/finish_scrin_headquarter_tips.py` (derivative only);
YAML generation uses `tools/art/bind_scrin_building_pngs.py`.
