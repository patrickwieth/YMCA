# Continents selection crash and pending coast work

Reference: `operational-continents-players-4-seed-1673296796.oramap`.

## Selection crash fixed

The map contains two `oilb` actors. OILB inherited `IsometricSelectable` through its building base and additionally declared `Selectable`. Mouse selection's `TraitInfoOrDefault<ISelectableInfo>()` therefore threw `TypeDictionary contains multiple instances`.

The default/map rules audit found eleven affected concrete definitions: HOSP, MISS, OILB, NKOREA_BUNKER, five China bunker definitions and two propaganda tower definitions. The local priority/class/bounds overrides now merge into the inherited isometric trait instead of adding a second selection implementation. OILB.indestructible removes the isometric selector and its dependent decorations, retaining its non-selectable intent. No engine lookup was weakened to silently pick an arbitrary trait.

`--check-selection-traits [MAP]` audits every concrete actor, including dynamically producible units/buildings, and exercises the exact crashing selection-priority lookup with None/Ctrl/Alt modifiers. The user's map passes after the fix. The original saved map is untouched; reloading rules is sufficient. Actual client captures of a diagnostic copy loaded successfully.

## Coast rendering is NOT yet accepted

Continents uses the flat rock-coast pipeline, not the height-four mountain pipeline recently improved for Hill Fortress/Battle Nexus. The previous global-appearance statement was too broad.

Investigation confirms that `naturalJoints`/low rear ends were enabled for Two Sides only. A candidate enabled those shared joins for all generated coasts and the editor. The resulting map had byte-identical terrain/resources/heights, but the visual comparison still showed short isolated rock clusters. The candidate was **reverted**, not presented as a complete fix or installed.

The reference export accepts fourteen short coast components and rejects other candidates because of protected landings/resources, narrow water rows, unsupported narrow spits or end footprints. Merely changing sprites does not solve this contour fragmentation. The remaining work is shared coast planning/fitting, without filling channels, hiding blockers, removing protected resources, closing landing access or regressing the approved Two Sides banks. Water Basins must remain cliff-free.

Evidence: `%TEMP%/ymca-continents-feedback` (`selection-before.log`, `selection-after.log`, `before.png`, rejected `after.png`, export diagnostics). The first screenshot fixture accidentally duplicated the Rules root and failed loading; corrected fixtures loaded normally. No coast completion, movement/balance or all-preset visual acceptance is claimed here.
