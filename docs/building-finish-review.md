# Building follow-up completion — local Game worktree

No release, push, deployment or interruption of the user's game. Current source
and YAML changes are loaded on the next `tools/test-game.cmd` start; that launcher
builds the required CA assembly first. Validation used an isolated build/runtime.

## Construction registration

The final `sequences/construction-alignment.yaml` override aligns existing shared
China construction stages with the finished building's ground location. It does
not move final art, resize construction graphics, change clocks/stages, queue
build times, reverse-selling behavior or completion callbacks.

For PNGs, the ground anchor is the center of the visible isometric ground diamond:
alpha-bottom minus one quarter of alpha-width, with source centering, transparent
margins, resolved Anchor/Offset and Scale applied. Native legacy sprite anchors
include actual engine sprite offsets and scale. Ground centers, rather than tall
sprite bounding-box centers, are aligned. There are 117 PNG and 34 legacy image
namespaces, including aliases. Naval make traits stay removed; Scrin's existing
hidden construction visuals remain hidden. Original graphics are untouched.

`prepare_construction_alignment.py` takes a resolved sequence audit and the output
of `--export-legacy-construction-anchors`. The selected construction stage family
is intentionally retained after footprint refinements; the engine checker verifies
that family's actual frame count and scale rather than inferring a different
animation duration from a newly reduced footprint.

Native start/middle/completion captures cover Helipad, China Supply Depot, Soviet
Nuclear Plant and Barracks, Allied Tech Center, Nod Repair Depot, GDI War Factory,
Warp Sphere, Swiss Bank and Grand Cannon at native world zoom 1 and UI scale 1.25.
The screenshots show the generic construction site, not a newly modeled buildup
of each building; differing construction/final-building sizes are intentional.

## Swiss Bank faction foundations

The bank retains its body, 3x3 site/occupied cells, pricing, health, power and cash
transfer behavior. Only its decorative foundation is selected by builder faction:
Allied, Soviet/China, GDI, Nod or Scrin. All playable subfactions are covered.
FactionInit is respected, consistent with existing faction-specific buildings;
otherwise the initial owner's faction is used. Capturing a bank does not switch its
builder's foundation style. Player-color masks still follow the current owner.

The inherited Allied body/collision/selection configuration is preserved, but its
unconditional overlay is replaced by mutually exclusive faction overlays. The
same body-local offset `-1134,0,0` centers each bib on the site. A single
WithBibRemap supports optional additional prefix/palette mappings, so GDI joints,
Nod lower edges and Soviet/China accents retain their own palette treatments.
Bib connection rendering uses the selected palette, not a hardcoded Allied one.
Other buildings retain the old default mapping exactly.

`WithFactionBibOverlayInfo` also selects the correct placement/actor-preview bib
using the preview's faction initializer rather than treating a faction condition
as always false. The corresponding remap preview uses the same selection.
Native fixtures created six banks with one owner but explicit different faction
initializers and verified exactly one appropriate live overlay and preview-family
selection for each. Their native foundation screenshots are archived.

## Footprints and traffic

- Allied Helipad: the raised deck/supports, tanks, stairs and hangar extend into
  the former passable rim. Final footprint is `xxx xxx xxx` on the unchanged 3x3
  site. Actual Mobile.CanEnterCell rejects all nine cells; an infantry unit walks
  successfully around the outside. Graphic registration, air docking and hit shape
  were not moved or changed.
- Soviet Nuclear Plant: 4x3 site with matching existing Soviet 4x3 foundation and
  collision/selection dimensions. Scale 1, cost 1500,
  HP 70000 and power 800 remain. The requested Nuclear Plant cameo is now present.
  Subsequent site-registration correction: sprite offset 3,-53, cell-axis selection
  and compensation for the shared bib's service-depot offset; see `map-biome-followup.md`.
- China Supply Depot: 3x3 with four external service bays, two per side. Main/free
  spawn `(2,3)`, helper bays `(1,3),(0,-1),(1,-1)`; DockOffset `-724,2172,0`.
  The body center is identical to the old compensated 4x3 center. Keep both outside
  approaches clear. Live traffic passed 42 deliveries/departures across all six
  refinery types, including China's twelve trips at its four docks, with the
  friendly minefield condition enabled.

## Cameos and regressions

Soviet Barracks/Nuclear Plant are no longer legacy exceptions: 99 faction portraits,
81 direct PNG sources. Latest approved shifts include Cloning Vats, Ore Purifier,
Pillbox, Prism Tower and the fourth Nod Repair Depot adjustment. Its current crop is
`[13,54,179,179]`; engine resolution confirms rep.nod uses rep-nod.png.

Earlier baselines remain frozen. Later explicitly authorized icon changes are
recorded separately, with before/after hashes and current-pixel checks, rather than
silently rewriting initial approvals. Focused suites cover all supplied art,
bindings, geometry, preserved economics, faction coverage, native results and
construction-only sequence changes. The isolated build and engine construction/
docking checks pass. This is not a claim that unrelated historical broad checks
(e.g. the old refinery-art expectation) have been repaired.

Evidence: `art-source/building-finish-review/`,
`art-source/construction-alignment/` and `art-source/soviet-nuclear-4x3/`.
`BuildingFinishReviewProbe` exists only in an environment-gated disposable map,
not normal game rules. All captured PNGs were checked for complete IEND before
archival. Test maps and source graphics were not overwritten.

Archipelago is accepted for now; only long-term playtesting remains. Continents
and Migration remain accepted. Deferred Scrin artwork/player-color decisions,
optional optimizations and release preparation remain separate.
