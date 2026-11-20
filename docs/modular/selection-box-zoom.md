# Configurable selection-frame zoom cutoff

Implemented and tested locally in Game / isometric after v0.97.5. Not committed,
pushed or released as part of this follow-up.

## Player behavior

Settings → Display → **Hide Selection Boxes When Zoomed Out**:

- Default: hide below 100% battlefield zoom (updated at the player's request).
- Slider range: 0–200%, in 5 percentage-point steps.
- Zero: always show frames. At the chosen boundary frames remain visible.
- Takes effect immediately, without restarting; saved through the normal settings
  window. Display Reset restores 100% and updates the slider/label.
- Actual battlefield zoom is used, independent of UI scaling.

Only the rectangular/isometric per-actor frames are suppressed. Health/extra bars,
selection state, clicking, control groups and the screen-space drag-selection rectangle
are not changed. The shared annotation renderers cover infantry, vehicles, buildings,
colored selection frames and rollover/target selection annotations.

## Implementation

`Game.SelectionBoxMinimumZoom` is a local client preference, not YAML actor state or
networked simulation state. Out-of-range preferences are clamped; non-finite values
use the default. A small boundary tolerance avoids floating-point zoom flicker.

The engine patch `tools/patches/selection-box-zoom.patch` contains Settings, the
optional display-panel slider binding/reset, and both annotation-render guards.
`mods/ca/chrome/settings-display.yaml` supplies the actual visible control.
The shared patch manifest installs it for normal Windows and shell builds; engine
and mod were rebuilt together successfully. No release-worktree changes were made.

## Validation

- `--check-selection-zoom`: default/custom boundaries, always-show, invalid values,
  isolated settings save/load and reset persistence.
- Opt-in native fixture: six infantry, one tank and one building remain selected
  with far frames hidden, far frames enabled, and close frames visible. Native
  settings panel slider updates the real preference and label; real display-panel
  reset restores both preference and slider. Saved file contains the slider value.
- Screenshot pixel check: far hidden/shown frame pixels 0/1950; identical 468 health
  bar pixels in both. Close view has frames again. All eight actors remain selected.
- Normal Release engine/mod build, 365 Modular tests and fresh/idempotent/conflicting
  Windows patch-preparation tests pass.
- Evidence: `art-source/selection-box-zoom/`, native run `.tmp/selection-zoom04`.

Reproduce after a normal build:

```
powershell -File tools/test-selection-zoom.ps1 -EngineDirectory ENGINE -OutputDirectory FRESH_PATH
python tools/test_selection_zoom_pixels.py FRESH_PATH
```

The launcher uses isolated support/settings/maps and requires the process-local
`YMCA_SELECTION_ZOOM_PROBE=1` gate. It stops only its own process. The fixture pins
its camera/zoom and disables its own world-click controller to prevent unrelated
desktop input from invalidating the screenshot comparison. Real game input is not
modified by the feature.
