# CI engine patch and build failure propagation

Diagnosed run: https://github.com/patrickwieth/YMCA/actions/runs/43052455295
Commit: `4e09133773f5958078c36eb2e58e666dfd151f6f`, branch `modular`.

The Linux .NET log contains nine CS1061 errors: the downloaded engine lacks
`DecorationFootprint` / `DecorationFootprintExtraDepth`, while the mod already
references them. Its build step nevertheless reported success because GNU find's
`-exec ... \;` does not propagate the invoked compiler's exit status. The Windows
entry point similarly only printed a failed-build message. The reported failing
step was later, during Lua installation, with a temporary Ubuntu mirror DNS error.

Changes:

- All three CI jobs fetch the pinned engine and apply the tracked selection-decoration
  patch before compiling. A reverse check accepts an already-patched tree; an
  incompatible patch fails rather than silently skipping required changes.
- Bootstrap performs the same version setup as the engine's `make version` directly
  through its packaging functions, avoiding a GNU make prerequisite in Windows Git
  Bash. HTTP failures now fail the download, with three transient retries.
- .NET and Mono solution recipes use fail-fast shell loops, preserving quoted paths.
- PowerShell throws on failed mod builds and nonzero engine command results.
- Lua setup refreshes package indexes and uses three acquisition retries; persistent
  failures still fail CI. This cannot guarantee external DNS availability.
- Checkout/setup-dotnet use supported Node 24 action versions (v6/v5).
- The conditional TournamentBot test step builds its own project (it is not in
  `YMCA.sln`) and propagates the test exit code.

Validation in `%TEMP%/ymca-ci-fix`:

- Fresh download of the pinned engine into an isolated directory; patch applies,
  reverse check succeeds, and full Release build succeeds without the existing
  locally patched engine.
- Actual Windows `make.ps1 all` entry point succeeds in that isolated checkout.
- Two selection-decoration tests, thirteen TournamentBot tests and 307 Modular tests pass.
- Negative PowerShell build test throws on a mocked compiler exit 23.
- `tools/test-ci-build-guards.ts` executes both actual solution-loop recipes with
  successful/failing mock compilers, two solutions and a filename containing spaces.
- `actionlint` passes (external shellcheck/pyflakes disabled); shell syntax and
  TypeScript regression-test checks pass.

The hosted Linux/Mono jobs and Ubuntu package-server DNS have not been rerun from
this local Windows environment. The GitHub run only reflects the fix after it is
committed/pushed and a new run completes. No release, deployment or engine-version
change is involved.
