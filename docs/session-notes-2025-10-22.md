# Session Notes 2025-10-22

## Hover Tooltip Investigation
- Commander tree now sets TooltipTemplate to COMMANDER_PRODUCTION_TOOLTIP and logs confirm we hit CommanderTreeWidget.SetTooltip with commander actors.
- ProductionTooltipLogicCA still never instantiates (no ctor/BeforeRender logs, no DBG: prefixes), so the commander tooltip path bypasses the CA logic entirely.
- Commander hover appears to route through the Cameo palette tooltip (ProductionTooltipCameoLogic) or a related cameo-specific widget; 	ooltipIcon is resolved, but the Cameo logic owns the rendering.
- Direct attempts to hook ProductionTooltipLogicCA from CommanderTreeWidget (lazy instantiation + Game.RunAfterTick) did not trigger; tooltip container children are rebuilt each frame.
- Commander tooltip extras therefore must be wired into the Cameo tooltip logic – that is where TooltipExtrasResolver should plug in so we can reuse shared formatting and localization.

## Next Steps
1. Trace the Commander tree palette icon creation to the exact cameo tooltip logic (ProductionTooltipCameoLogic/PaletteIconTooltipCameoLogic) and integrate TooltipExtrasResolver there.
2. Ensure hover template references still point to CA chrome but that the cameo logic populates the labels (Strengths/Weaknesses/Attributes) using localized strings.
3. Once wired, rerun 	ools/test-game.cmd and verify commander hover shows tooltip extras while regular build menu tooltips remain intact.
4. Remove temporary debug logging (CommanderTreeWidget/ProductionTooltipLogicCA) after confirmation to keep logs clean.
