# Sentry V9 — spatial lighting rather than more surface speckles

Review `comparison-3x.png`: source / V8 emission-only / V9 lighting.
`base-assembly-preview-3x.png` composites onto the untouched supplied base.
`rotation-check-2x.png` checks the same fixed light at four turret orientations.
Editable model: `sentry-v9-lighting.blend` (Blender 3.6 / Eevee).

Actual rendering changes:
- Fixed world-space directional key light, hard cast shadows and contact shadows.
- Ambient occlusion around overlapping parts and small recesses.
- Diffuse response instead of pure emission, retaining a controlled emission floor
  so already shaded source pixels are not crushed to black.
- Separate compensation for the side plate/emblem and bearing, whose source
  artwork already contains strong baked shading.
- Restrained glossy response on metal to create narrow edge/tube highlights.
- Palette mapping without dithering. Neutral source metal stays in the neutral
  subset of the original palette instead of becoming olive/brown from scenery colors.

The first lighting test over-darkened the emblem/side plate. It was reviewed,
corrected and rerendered. Neither a global contrast filter nor more painted wear
was used to produce the spatial shading.

## Important palette distinction

Unlike V8, the RAW physical-lighting render DOES contain new intermediate RGB
values. `compare_sentry_v9.py` maps these to exact existing source colors, with
binary alpha and no dithering. Thus the FINAL `turret-*.png` and comparison images
of the turret use only source colors; a direct Blender render is an intermediate,
not the finished palette-constrained sprite. The source texture images themselves
have not been recolored or replaced.

`palette-audit.json`: 52/47/53/51 final visible colors at 0/45/180/315 degrees,
0 colors outside the original in every view. The raw renders contain
360/212/394/281 RGB colors respectively.

Model vertices/topology/transforms/visibility and camera remain unchanged, verified
by before/after hashes. All four alpha silhouettes match V8 exactly. No clipping.
No gameplay integration, final 32-facing export, commit or push.

```text
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v9.py
python tools/art/compare_sentry_v9.py
```

This is a lighting improvement study, not a claim of full reference equivalence.
Some depth in the original is hand-authored into individual pixel clusters and
cannot be recovered solely by adding a generic light. Source placement and local
material balance still need review. Prior attempts and all supplied artwork remain intact.
