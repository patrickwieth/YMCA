# v0.97.2 release preparation

User authorized commits, pushes, the next release, and tournament-bot deployment.
The release is based on `tournament-bot`, not an isometric branch merge. Experimental
foundation/art work stays on `isometric`; the independently checked Aegis socket fix
is applicable to the existing released 128x64 projection and unchanged ship sprites.
Unrelated working files and the protected Nod conyard PNG must not be staged.

Validated locally:
- Release mod assemblies built against a separate pinned, patched engine (no errors).
- Modular suite: 319 passed; tournament suite: 20 passed.
- Dedicated integration rerun against release sources: admin wizard, unauthorized
  request rejection, identical map on admin/guest/late joiner. UID:
  `861f3d85ec203b5c710d67f0f6dba04acdcf6f61`.
- Explicit generated tournament host tests: two and four players, real headless
  server startup, exact map UID and verified SHA-256 HTTP package download.
- Battlefield News native check passed, including bundled announcement, canonical
  feed/cache, HTTPS links, malformed feeds and offline cache handling.
- Main-menu Kirov interval: 14 seconds; initial formation and cap preserved.
- Aegis before/after and complete hull/turret rotation reviewed in a separate
  native client; original sprite files and weapon definitions remain unchanged.

Tournament deployment builds a v0.97.2-compatible host. Existing match-safety
checks remain enabled and run again after the potentially lengthy Docker build.
Deployment additionally waits for Discord command registration from the current
container, rather than treating a live HTTP listener as proof of a working bot.
Generated tournament maps currently require the join-page download/install step;
dedicated lobby recipe synchronization does not replace that tournament workflow.

## Verified publication and deployment

- Release: https://github.com/patrickwieth/YMCA/releases/tag/v0.97.2
- Packaging/deployment run `44201770748`: all three platforms, stable manifest and
  game-server deployment completed successfully. Stable manifest names v0.97.2.
- Public YMCA servers at `45.136.28.158:1234` and `:1235` advertised `ca@v0.97.2`
  after deployment; this was checked separately from the workflow result.
- Tournament run `44205277452` initially failed building the old Debian Bullseye
  image because its referenced security packages returned HTTP 404. It did not
  replace the existing bot. SDK/runtime images were changed to .NET 6 on Ubuntu
  Jammy without changing the game's engine pin or target framework.
- Tournament retry `44205681767` succeeded. Its checks confirmed Discord connection,
  command registration and host version v0.97.2. The public HTTPS join route also
  returned the expected application-level 404 for the synthetic `health-check`
  match, not a proxy error. No real tournament was created just for this check.
- Separately deployed bot hardening locks each lobby to its assigned map and
  excludes live state/token files from Docker build context. The updated bot suite
  passed 21 tests and both real generated-host smoke tests passed again.
- Isometric source changes were committed/pushed separately; experimental bib art
  is not in this stable package. Unrelated working assets, map deletions and the
  protected Nod conyard PNG were not committed.

No match-safety override was used. Existing user game processes were not stopped.
