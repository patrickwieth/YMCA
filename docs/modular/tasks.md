# Modular tasks

- [x] Replace checkpoint artwork with `mods/ca/bits/misc/buildings/research core.png` and display **Research Core**. Preserve `FCOM.checkpoint` IDs, capture, experience and victory mechanics. Dedicated checkpoint ruin restores the checkpoint; ordinary FCOM stays unchanged.
- [x] Audit added/replaced PNGs and supplied building art for uppercase paths. Normalize 15 filenames and update references; collision checks and exact Linux-style reference checks pass. Tracked case-only renames are recorded using Git. Protected Nod conyard remains untouched.
- [x] Finish GDI building PNG/bib bindings and resolve duplicate inheritance merge errors. Nine building types plus Nukular conyard integrated, with Nod isolation. Build and focused rules/art checks pass.
- [ ] Live visual review of GDI buildings/bibs and Research Core; refine initial offsets as needed.
- [ ] Review historical Allied APWR/Helipad alpha-contour baselines against user-replaced assets (6 subtest failures); do not overwrite artwork to satisfy old baselines.

Implementation and validation details: [gdi-building-art.md](gdi-building-art.md).
