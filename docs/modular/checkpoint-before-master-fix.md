# Modular checkpoint before master bugfix

The vehicle designer and agreed supply/energy design are saved on `modular`.
See `weapon-supply-energy-design.md` for agreed concepts versus implemented features.

## Source artwork checkpoint

User-supplied battery, conversion, pump and interface PNGs are source assets, not automatically
admitted components or implemented energy systems. No new item pictures were generated.

Original generator assets were renamed without pixel changes:
- `basic generator.png` -> `big mechanic to electric.png`
- `improved generator.png` -> `steam to electric.png`
- `small generator.png` -> `small mechanic to electric.png`

The baseline-generator artwork binding now reads the first renamed source. Both runtime component
atlases were repacked; the deleted small atlas was restored from the original supplied pixels.
Existing component IDs and gameplay remain unchanged by this artwork update.

## Unfinished rule work retained

Commit `2c1951af` saves the user's local China vehicle edits and GDI whitespace change separately.
These are a checkpoint, not a claim of completed gameplay validation. In particular, China edits
remove the Nuke Cannon shell actors/spawner and change prerequisites and variants. Existing ground/
air/naval inventory fingerprints and projectile-actor tests still describe the earlier rules.
Previously observed full Python suite failures: two inventory assertions and one missing shell-actor
lookup. Review gameplay and refresh the appropriate inventories/tests before treating this rule
batch as complete; do not silently rewrite calibration to hide the differences.

Artwork-focused checks pass (8 icon tests, 4 cannon-art tests). Full-mod/live validation remains open.
The protected `mods/ca/bits/nod/buildings/conyard.png` remains a local uncommitted modification.
Use a separate `master` worktree for the main-version fix rather than stashing or replacing it.
