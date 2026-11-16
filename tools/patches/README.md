# Local isometric engine extension

The generated `engine/` directory is ignored by Git. This checkpoint includes
`isometric-selection-decoration.patch` to preserve the local modular engine changes
alongside the mod code: selection geometry, sprite construction/preview behavior,
placement passability, production presentation, and the support-power/launch-URI
crash fixes. The patch also carries their engine regression tests.

The existing filename is retained because all three CI jobs already apply it.
It is a complete source delta against the pinned engine below, not a second patch
that must be stacked on top of the old selection-only version.

Base engine: `2dfb43c864804ede5efca9e31f7add52f7233aa3` (same source tree as
`484afea27c973b843edc09346b2bd0110370057e`).

The Continuous Integration workflow now fetches the engine and applies this patch
before building in all three jobs (.NET, Mono and Windows). A reverse check skips
an already-applied patch; an incompatible or partially applied patch fails the job
instead of overwriting local changes. See `docs/ci-engine-patch-fix.md`.

For manual builds after fetching a fresh engine, run from the Game repository root **before building**:

```sh
git apply --check --ignore-space-change tools/patches/isometric-selection-decoration.patch
git apply --ignore-space-change tools/patches/isometric-selection-decoration.patch
```

The current working engine already contains this patch. To check that case:

```sh
git apply --reverse --check --ignore-space-change tools/patches/isometric-selection-decoration.patch
```

Do not reapply it or automatically overwrite an independently modified engine.
