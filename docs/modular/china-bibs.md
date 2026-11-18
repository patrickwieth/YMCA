# Soviet-style China foundations

User-requested opt-in in Game/isometric. This supersedes the earlier deliberate
China exclusion from Soviet player-color masks. No release or branch integration.

`rules/china-scrin-building-art.yaml` enables `WithBibRemap` and the existing
Soviet `grid` sequence on China's foundation-bearing base actors. Derived actors
inherit these settings. Later supplied-art layers retain their existing bib sizes
and offsets. No building artwork, occupancy, weapons or other gameplay changed.

Resolved foundations use the same `sovietbib*` grid/remap assets and `bibplayer`
owner palette as Soviet buildings. The existing connection layer automatically
uses matching grid tiles and colored joints, including its single-cell diagonal
fill. Removing/capturing buildings updates connections as before.

Existing deliberate bib removals remain: supplied bunkers do not gain an unwanted
new foundation, and the shipyard is not given a land bib.

Validation:
- `--check-bib-connections` resolves sprites and verifies China's foundation
  actors/variants use the remap and grid; other-faction isolation remains checked.
- Native lifecycle test using `chpower` and `chpropagandatower`: cardinal/diagonal
  links, mixed-owner rejection, removal, capture, placement and destruction pass.
- Native before/after screenshots inspected in `art-source/china-bibs/`.
  The initial capture attempt was affected by viewport edge scrolling; the final
  test disables it and shows the actual foundations and connectors.

This is a rules-only runtime change using the existing bib implementation; the
utility assertion update does not add a new runtime dependency. Restart/reload
is required to see updated rules. The separate pending waiting-lobby DLL update
still needs installation before restarting the normal Game runtime.
