# Smaller Ion Defense previews — NOT installed

**Start here:** [size comparison](start-size-comparison.png).

Left to right, on one ground baseline and without display enlargement:

1. V4: old narrow model, 160px native canvas.
2. V5: currently installed, too large, 320px native canvas.
3. **V6 A:** broad V5 geometry directly rendered at **160px**.
4. **V6 B:** the same broad geometry directly rendered at **192px**.

The canvas number is not the visible building width. A restores the former overall
size while retaining the wider cross section; B is a 20% larger intermediate option.
Neither candidate replaces the Ion Defense game assets or rule offsets yet.

- [Reference and several directions](reference-and-directions.png): upper images
  are enlarged 2x for inspection; lower row is actual 160px native output.
- [32 native A views](all-32-native160.png).
- [A rotation preview, displayed 2x](rotation-native160-2x.gif).

Both candidates use new native Blender renders, not resized V5 turret PNGs. The
source-cut fixed base is recomposed directly at each candidate density. Base and
turret remain separate. A has all 32 directions; B has eight study directions.

This is **a size comparison**, not a claim that the unresolved model/UV quality
problems have been repaired. Hidden geometry is still inferred and some end-on
texture transitions remain imperfect. The user's open Blender scene was untouched.

Validation checks native frame sizes, binary alpha, original source palette and
no clipping. The active game PNGs are tested byte-for-byte against V5 exports.
The separate Guard Tower integration does not install either Ion candidate.

Reproduce from Game:

```sh
blender.exe --background --python tools/art/render_heavy_blaster_v6_preview.py
python tools/art/pack_heavy_blaster_v6_preview.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v6_preview.py
```
