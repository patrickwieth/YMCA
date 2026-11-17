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

Publishing must follow the committed tag workflow: three platform packages,
then stable manifest, then game servers. Run the separate tournament deployment
only after the release pipeline succeeds. Verify both workflow results and the
public join-page endpoint; do not infer deployment from release creation alone.
