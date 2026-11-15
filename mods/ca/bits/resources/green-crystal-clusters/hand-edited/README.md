# Hand-corrected final green motifs

These 16 PNGs preserve the user's corrections from the `outlined-*-body.png`
files. They are authoritative final art, not intermediate generated output.

Edit files **in this directory** for future corrections. To override another
active motif, copy its matching `outlined-*-body.png` here first. Keep the filename,
160x128 canvas, transparent background and ground placement unchanged.

`tools/prepare-green-crystal-clusters.ts` loads these overrides after automatic
color, earth and outline processing. It copies the PNG files byte-for-byte to the
parent directory and uses their decoded RGBA pixels for all density frames.
No additional recoloring, soil, outline, resizing or repositioning is applied.
`layouts.json` records override paths and SHA256 hashes under `handEdited`.

Regenerate using the normal green cluster command from the YMCA parent directory:

```sh
tsx Game/tools/prepare-green-crystal-clusters.ts '11 green crystals.png' Game/mods/ca/bits/resources
```

The script requires the existing external `tsx`/`pngjs` tool dependencies.
Changes to the parent directory's generated PNGs alone are still overwritten;
changes here survive regeneration. Blue sprites and trees are unaffected.
