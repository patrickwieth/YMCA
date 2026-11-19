# Foundation color comparison: GDI corners / Nod lower edges

User-requested local variants in Game/isometric. No push, release or production
branch integration. Other factions retain their existing treatments.

## GDI

Exactly the four outermost half-cell panel surfaces are colored. The fixed grid
has four panels per 1x1 cell, so a 1x1 bib has all four panel surfaces colored;
larger bibs retain neutral inner panels. Every original dark joint pixel is
excluded. Sizes 1x1, 2x2, 3x3, 4x4, 4x5 and the EMP-offset variant are supported.

Only the existing concrete surface color is masked. Corner selection uses the
native 64x32 panel grid, not per-image resizing. Gap filler tiles remain neutral
(`RemapConnections: false`) to avoid turning connecting strips into solid team
color. This option defaults true, preserving other factions' behavior.

## Nod

Only the existing lower vertical faces are colored: the bottom seven opaque
pixels per column, further restricted to the two authored side-face colors.
Top plates, surface detail, joints and transparent pixels remain unmodified.
Two palette luminances preserve a light/dark distinction between the faces.
Sizes 1x1, 2x2, 3x3 and 4x3 are supported. Nod gap fillers use the same edge mask.

## Binding and preservation

`rules/bib-player-color-variants.yaml` opts resolved foundation actors into the
appropriate mask family and palette, including derived Nod actors that otherwise
inherit GDI presentation. Nod explicitly re-enables connector masks in that
inheritance chain. Existing bib visibility/construction conditions are retained.

Indexed masks live beside the original bibs as `*-corner-remap.png` and
`*-edge-remap.png`. No original PNG is overwritten, especially not Nod conyard
art. GDI assets are referenced directly in the authoritative runtime folder.
The generated sequence file only adds `remap` sequences; it does not override
base idle filenames. EMP and rectangular Nod mask offsets match their bases.
Buildings, geometry, occupancy, gameplay and approved AA artwork are unchanged.

Generate: `python tools/art/generate_bib_color_variants.py`.
Test: `python tools/test_bib_color_variants.py` and native
`--check-bib-connections` (effective bindings, mask registration, all actor
variants, palette families, construction order, connector policy).

Native GDI and Nod lifecycle tests passed cardinal/diagonal linking, capture,
removal, new placement and destruction. Actual initial and post-capture renders
were inspected. Evidence and source hashes: `art-source/bib-color-variants/`.
The initial Nod test used the obsolete `nod` faction identifier; correcting the
fixture to `blackh` activated its existing faction-gated bib. No faction gating
was changed in production rules to make the test pass.

The destructive local diagnostic maps are removed from normal runtime map lists
once testing finishes. Updated CA/Cameo assemblies are staged under
`.art-validation/allied-bib-remap/runtime/bin/`; close the user's game before
running that directory's parent `install.ps1`. New rules include the connector
mask option and require the updated CA assembly. The running game was not stopped.
