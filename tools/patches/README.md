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

Windows `make.ps1 all` / `check` prepares the complete patch stack before compiling
both an existing and a newly downloaded engine. PowerShell and shell preparation
share the ordered list in `tools/engine-patches.txt`. Git for Windows must be on PATH.
Repeated preparation is a no-op; conflicting local source aborts before compilation.

This fixes the local `IReplayMapProvider` build failure caused by a mod using the
portable replay API while the generated engine still lacked its patch. The actual
`Game/engine` and mod were rebuilt together in Release successfully. Regression
coverage: `python tools/test_windows_engine_patches.py -v` (fresh engine, paths with
spaces, repeated preparation, and preserving conflicting local edits).

For manual builds outside `make.ps1`, run from the Game repository root **before building**:

```sh
sh tools/prepare-engine-patches.sh
```

The current working engine already contains this patch. To check that case:

```sh
git apply --reverse --check --ignore-space-change tools/patches/isometric-selection-decoration.patch
```

Do not reapply it or automatically overwrite an independently modified engine.

## Portable replay maps (local follow-up)

`portable-replay-maps.patch` is applied after the existing selection and legacy-map
patches by `tools/prepare-engine-patches.sh`. It adds optional embedded map data,
mod-provided recovery hooks and missing-map replay-browser support. Rebuild the
engine and mods together; installing only the new CA DLL into an old engine is
not supported. See `docs/portable-replay-maps.md`.

## Additional v0.97.4 corrections

- `stable-generated-map-folder.patch`: version stamping must preserve the stable
  generated-map directory used by tournament hosting and clients.
- `appimage-link-registration.patch`: opt-in user-local `ymca://` desktop handler;
  the package builder also installs `packaging/linux/register-links.sh`.
- `map-cache-local-priority.patch`: delayed repository responses cannot replace a
  locally installed map. Remote metadata is committed atomically before callbacks.

## Tournament post-game statistics window

`tournament-postgame-replay.patch` publishes the final replay independently of the
configured post-game shutdown delay and lets ExitOnGameOver dedicated processes
exit after their server thread has completed cleanup. The tournament bot uses a
180-second statistics window while processing results promptly. Rebuild the
engine/host together; see `docs/tournament-web-and-postgame.md` for native evidence.

## Rock occlusion rendering

`rock-silhouette-model.patch` exposes the existing resource drawing pass for the
mod's occlusion-aware subclass and permits shadow-free voxel silhouette copies.
Normal model rendering is unchanged. The shared patch list includes this extension
for Windows and shell builds; rebuild engine and mod together. See
`docs/modular/rock-occlusion.md` for native validation and scope.

The preparation script applies all patches and rejects incompatible local source.
The complete stack has been checked against the pinned engine, including repeat
application detection. See `docs/release-0.97.4-validation.md` for test evidence.
