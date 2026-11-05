# Local isometric engine extension

The generated `engine/` directory is ignored by Git. This checkpoint includes
`isometric-selection-decoration.patch` to preserve the local decoration-footprint
extension and its two engine tests alongside the mod code.

Base engine: `2dfb43c864804ede5efca9e31f7add52f7233aa3` (same source tree as
`484afea27c973b843edc09346b2bd0110370057e`).

After fetching a fresh engine, run from the Game repository root **before building**:

```sh
git apply --check --ignore-space-change tools/patches/isometric-selection-decoration.patch
git apply --ignore-space-change tools/patches/isometric-selection-decoration.patch
```

The current working engine already contains this patch. To check that case:

```sh
git apply --reverse --check --ignore-space-change tools/patches/isometric-selection-decoration.patch
```

Do not reapply it or automatically overwrite an independently modified engine.
