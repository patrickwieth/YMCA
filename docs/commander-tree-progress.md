# Commander Tree Integration Progress

## Completed
- Wired the existing sidebar promotion button (ClassicProductionLogicCA) to open the new COMMANDER_TREE_OVERLAY window via Game.OpenWindow, reusing the active world/renderer so the widget gets full context.
- Added the new chrome window at mods/ca/chrome/commander-tree-window.yaml, including centered dialog, close button, tooltip container, scroll panel, and commander tree widget instance.
- Implemented commander tree UI (OpenRA.Mods.CA/Widgets/CommanderTreeWidget.cs) and window logic (CommanderTreeWindowLogic.cs) with tooltip, commander points display, close/dismiss handlers, and scrolling support.
- Rebuilt layout logic to:
  - Collapse vertical spacing to at most one icon height between rows.
  - Pack leaf-only branches into rows of up to three icons, reducing overlay height.
  - Track row ranges (RowTop/RowBottom) and leaf column slots for consistent anchoring.
- Restored production overlays and text (clock, queued count, ready/hold text, infinite symbol) by caching IProductionIconOverlay instances and rendering overlay fonts inside the commander tree widget.
- Reattached production tooltips so hover refreshes the tooltip container with commander-specific icons.
- Ensured scroll events bubble to the parent ScrollPanel, so mouse wheel scrolling works without crashes.

- Added cloak_voxel base and player palette definitions (mods/ca/rules/palettes.yaml) so commander tree overlays reuse the production palette sprites without palette load crashes.
- Hooked filler-provided prerequisites into edge building so upgrades unlocked via filler chains now anchor immediately to the right of their parent nodes.
- Forced commander tree hover handling to refresh the PRODUCTION_TOOLTIP as soon as the cursor enters the widget, restoring production tooltip visibility without extra mouse movement.
- Routed the commander tree to its own tooltip container so the production tooltip matches palette behavior and renders above the overlay.
- Show all promotion descendants up front, keeping future upgrades greyed out but aligned with their parent, and dim linking edges until nodes are revealed.
- Added localized ready/hold strings for commander tree production overlays, replacing hardcoded fallback text.
- Added commander tooltip fallback lookup + logging so decorated promotion IDs reuse base TooltipExtras and surface issues when data is missing.
- Restored commander tooltip extras by resolving canonical actor definitions when promotion queue entries use decorated names.

## Outstanding Issues
- Verify commander point labels update immediately after purchases (currently assumed but not retested post-refactor).

## Next Steps
1. Exercise commander trees across multiple factions/maps to ensure overlay palettes remain available and no palette-specific crashes occur.
2. Play through commander unlock paths across multiple factions to confirm the new prerequisite edge mapping keeps child nodes aligned and avoids regressions.
3. Re-run 	ools/test-game.cmd after the above fixes to confirm scrolling, overlays, tooltips, and overall stability remain intact.

## Notes
- Progress to date lives in OpenRA.Mods.CA/Widgets/CommanderTreeWidget.cs (~700 LOC) and associated chrome entries. Most recent changes focus on layout and overlay rendering.
- Scroll handling depends on the tree widget returning alse for MouseInputEvent.Scroll; any future custom handling must continue forwarding to the parent scroll panel.







