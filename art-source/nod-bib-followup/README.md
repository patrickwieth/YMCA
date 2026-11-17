# Building-position feedback and native Nod4x3 bib — local only

`position-feedback.json` records every requested screen-pixel translation and the
resulting sequence, selection and status-overlay offsets. The late rules/sequences
`building-position-feedback.yaml` preserve source PNGs and earlier calibrations.
Reproduce with tools/art/apply_building_position_feedback.py. Before-rule/sequence
snapshots are immutable inputs; do not recapture them from an already-adjusted mod.

## Explicit requests

- GDI Conyard/Barracks/Weapons Factory/Repair Facility:4px down.
- GDI Advanced Power Plant:4px up.
- GDI Comm. Center:20px right/down; Refinery and Advanced Comm. Center:40px right/down.
- GDI Tech Center/Airfield/Upgrade Center:10px down.
- Soviet Industrial Plant/Refinery:20px down; Service Depot:20px right/down.
- Soviet Tech Center:10px down; Conyard/Power Plant:10px up; Nuclear Plant:4px down.
- Soviet Barracks: extra bib removed, short00000070 silhouette shadow added using
  the existing allied shadow behavior,4px down. Actor placement/footprint unchanged.
- Allied APWR renamed Fusion Power Plant, with no cost/power/death-weapon change.
  Soviet APWR retains its nuclear name and previously approved1500/800 stats.
- Soviet Power Plant cameo: explicit icon offset0,0 prevents inheriting the building's
  Defaults.Offset0,-38. The actual sidebar sprite was loaded and checked centered.

## Nod

Native4x3 bib448x231 is cut from two copies of the original3x3 using integer64,32
translation and a binary dark-plate seam. Every visible pixel is an original source
pixel. No resizing, recoloring or filler. Runtime file bits/nod/buildings/bib4x3.png
is directly referenced by nodbib4x3 and WEAP.NOD. Bottom32,16 retains the original
rear/left cell anchor and adds one cell toward the right/front. Factory body, actual
3x3 occupied footprint, exits, production and actor center remain unchanged.

Conyard moved8px higher, with selection and status effects following. Reviewed rail
endpoints previously exceeded the front-left top plane by6px; they now clear it by
2px. Supplied conyard.png is NOT rewritten. See nod-before-after.png (native composed
review, not a live screenshot). Source/map hashes are in protected-before.json.

## Scrin / Gatling follow-ups

See ../scrin-rear-alignment/README.md and ../chinese gatling/ring-alignment/README.md.
Unrelated Nod-derived status effects are explicitly pinned so GDI offsets do not
leak through shared inheritance. Body/selection/effects move relative to stationary
bibs; no Building/Exit/Docking/weapon values are rewritten by this position layer.

## Validation

Dedicated test_building_position_feedback.py compares resolved gameplay fields with
snapshots, verifies exact requested offsets and native Nod source pixels. Engine
loads the actual Nod4x3 asset; the current grid audit remains aligned. Gatling tests
and the engine muzzle-projection checker cover the separately supplied ring change.
The normal test-game.cmd --check-only build/map preparation passed before the latest
engine/occupancy follow-up. The running game subsequently locked the live DLL; the
latest build/checks therefore use an isolated runtime. Authoritative map unchanged. Historical pre-feedback art baselines describe earlier poses,
not this explicitly requested batch. Live review after restart remains pending.
No commits, pushes, releases, deployments or process termination.
