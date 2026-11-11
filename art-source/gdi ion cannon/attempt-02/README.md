# V2 — first source-detail / UV correction

Separate from preserved V1; no game binding or gameplay changes.

Implemented:
- Replaced the mixed, stretched barrel crop with separate armor, upper bevel,
  copper underside and cyan-conduit source regions.
- Mapped source rivets, edge highlights and conduit detail onto the support faces.
- Split receiver windows and vent armor into distinct source patches.
- Added source roof/slope detail and a bent roof-conduit surface.
- Replaced the blank black base top with source plating; mapped front gear/panel
  and side pipe details without duplicating the same small crop across panels.
- Removed the incorrect broad brown bearing-top color.
- Removed contrast/AO crushing for this texture inspection pass.

`uv-provenance.json` records explicit reference-pixel landmarks for every new
textured face. All sample the untouched 1024px original with nearest interpolation.
The blend packs the source image. No procedural noise/weathering or painted detail
was added. Raw renders are palette-mapped and given a one-pixel exterior contour.

Files:
- `heavy-blaster-v2.blend`: saved independent scene.
- `comparison-2x.png`: reference / rejected V1 / V2 at equal study pixel density.
- `front-128.png`: directly rendered 128px inspection image, NOT a resized 320px PNG.
- `native-review-4x.png`: nearest enlargement of that direct render.
- `validation.json`: source hash, palette, binary alpha and no-clipping checks.

Still visibly unfinished: the barrel remains too open, the receiver too boxy,
base armor too flat and the bearing too exposed. Some UVs distort details because
V1 geometry does not match the reference. Capacitors/trunnions remain simplified.
Hidden/rear surfaces are not revised or validated; no 32-facing export is claimed.
Texture detail is improved but this is not a finished reconstruction.

Reproduce from Game:

```
blender.exe --background --python tools/art/refine_heavy_blaster_v2.py
python tools/art/review_heavy_blaster_v2.py
```

Separate background Blender process only; the user's open scene is untouched.
