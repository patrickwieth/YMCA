# Helipad — centered visual foundation registration

User reported the helipad sitting too high relative to its bib. The prior generic
calibration aligned its small ground contacts against the rear edge of a 3x3 bib,
leaving its visual ground envelope 43px above the foundation center.

All five body states now use Bottom offset **-1,-25** instead of -1,-68. Selection
decoration Y moves from -973 to 0, with its width/height unchanged. The actual
3x3 bib stays fixed; no sprite rescaling or source edit. The calibration policy
for HPAD.TD is now explicitly `centered-visual-ground-envelope` rather than rear
edge alignment. Re-running the calibration reproduces this adjustment.

[Before/after source composite](bib-centering.png), not a game screenshot.

Resolved before snapshots are retained here. Tests verify that no actor traits
other than the visual selection translation changed, and only five body sequence
offsets changed. Building dimensions 2x3, LocalCenterOffset, exits, aircraft spawn,
landing/docking/production and hit/mouse bounds remain untouched.

Guard Tower simultaneously received its separate **gdibib1x1** decorative overlay,
using the existing 128x64 source at Scale1. Legacy bib remains removed; the actor's
occupied 1x1 footprint and gameplay are unchanged.

Release build, real Guard Tower/bib loader, GDI-art and placement/alignment tests
pass. Live review pending; no game restart, commit, push or deployment.
