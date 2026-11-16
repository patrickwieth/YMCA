# Scrin bib installation — local runtime bindings

The user explicitly requested installation after seam refinement. Promoted the exact
reviewed1x1,2x2,3x3 RGBA cut/paste outputs into `mods/ca/bits/scrin/buildings/`.
`binding.json` records the actor/size mapping and promoted file hashes.

The top plating surface, not the bottom of its raised front lip, is the registration
plane. Native Bottom0,23 gives sprite offset0,10 for all three canvases and places the
reviewed top-surface center at the actor origin. This supersedes the early0,-1 lower-lip
candidate. Scale1, neutral effect palette, ZOffset-4096, decorative and hidden during
construction. No source rescaling/recoloring; no building relocation in this step.

Land structures and defenses use their corresponding sizes. SFAC.Nukular and SCOL.Temp
inherit correctly. The disabled legacy silo also receives1x1 plating, replacing its
legacy bib. Its duplicate unnamed Inherits entries needed distinct keys so the added
rule layer can merge; resolved actor fields otherwise remain identical. STRM is an
effect, not a building, and is excluded. No naval bib is introduced.

Validation:
- `--check-scrin-bibs`: real engine PNG loading, source RGBA equality, resolved native
  anchors, and exactly one Scrin bib on each actor/variant with no legacy bib.
- `test_scrin_bib_installation.py`: source/protected hashes, full resolved gameplay
  equivalence apart from decorative bibs, states/anchors/depth.
- Installed Shard30 and Plasma05 regressions pass; their sprites, roots, pivots,
  barrels, muzzles and weapons are untouched. Material43 remains uninstalled.
- `placement-review.png` is a native composed review, NOT a live screenshot.

A subsequent global grid-registration pass now translates all decorative bibs together
with their actor origins and attachments; see `art-source/bib-grid-alignment/README.md`.
This preserves the relative body/bib contact registration established here. Live appearance needs restart/new match; no game was
stopped and no runtime DLL overwritten. No commit, push or release.
