# Black upper cliff lines — Mountain Valleys feedback

Reference: `screenshots/schwarze linien.png` in the parent workspace. Reproduced on a Mountain Valleys 4P42 ramp and rear crest using unchanged exported terrain.

## Changes

- Closed-cliff rear clipping now stops three source pixels below the roof socket, rather than four. The fourth row was opaque black source matte. The existing raised stone crest remains; no closed-cliff PNGs or original artwork are modified.
- Thin rear retaining-wall profiles retain their exact alpha silhouettes but sample exposed authored stone instead of the rear source's dark interior fade.
- The older projected expansion-wall family uses the same exposed-stone approach on its rear profiles and rear corner overlays. Its front art and all masks remain unchanged.
- The shared renderer applies the clipping correction to game and editor previews. No map-generation, terrain, actor-placement, height, resource or collision changes are part of this fix.

This addresses the upper black wires, not every dark detail in the rock. Vertical front-piece seams visible in the reference are a separate issue; front RGBA is deliberately preserved. Fragmented expansion-wall topology and the pending island-coast topology issues are not repaired here.

## Checks

Evidence: `%TEMP%/ymca-black-cliff-lines`.

- `--rubberduck-rear-line-test`: all six straight rear variants remove 65 matte pixels from the displayed contact band, retaining at least 95% of its opaque stone pixels. Isolated natural dark details are not globally color-keyed away.
- Native-face generation checks all 150 front frames for exact RGBA preservation, all rear alpha masks for equality, continuous contact and unchanged profile bounds.
- Mountain-face generation checks unchanged front RGBA, all alpha and all transparent pixels in both atlases. The shared visual suite passes 36 distinct face variants and 16 bounded corner overlays.
- Candidate `map.bin` is byte-identical to its baseline. Real normal/shaded client captures and the actual editor pass corner/cache updates, Undo/Redo and terrain/resource/actor save/reload.
- General contour tests: 180 accepted, 543 corrupt rejections, five fixtures, 52 protected feet; Modular tests: 180/180. These are scoped checks, not new multiplayer/performance or balance certification.

The first editor diagnostic used an invalid result filename and was rerun with the required dedicated-directory SHA256 log name; its guarded failure is not counted as a pass.

Restart to reload the shared art and renderer. Existing generated maps using shared assets benefit without regeneration; maps embedding their own artwork retain those overrides. Source hashes and generated-art hashes are recorded in the installed provenance files.
