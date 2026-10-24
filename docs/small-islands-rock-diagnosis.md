# Small Islands rock report: historical diagnosis

**Update:** Archipelago / Small Islands now use [open shores without rock banks or their collision](map-rocks-and-dry-oil.md), as requested. This supersedes the unresolved report below for newly generated maps.

Exact user map copied read-only for diagnosis: `Operational Custom Islands/SmallIslands connections=False 4P 1573618755`, session package `ymca-session-d35869af1fab403b961236d5d898a138.oramap`.

Evidence: `%TEMP%/ymca-small-island-rocks` (original package, front/rear client captures and owned diagnostic fixtures).

The map contains 46 blocked bank cells and 103 coast actors across nine named coast groups. Actual front view at CPos 109,-6 reproduces repeated open square/diamond-like rock pockets and protruding disconnected-looking pieces. This is the older coastal composition path described in `preset-cliff-audit-and-island-bridges.md`, not missing Tiberium assets or an island-economy toggle failure.

The legacy renderer replaces neighboring faces at concave corners with standalone corner sprites and does not align mixed-direction components to the bank plane. A diagnostic-only reconstruction retained every face and used bank-aligned corner/face aliases (145 actors). Its front view improves, but its rear view at CPos 49,12 collapses into an excessively thin zigzag rim. This candidate is **not accepted or installed**. A global naturalJoints switch is not a sufficient fix.

All four diagnostic copies retained the exact original `map.bin` (terrain, heights and resources), SHA256 `03fc4d8e934c5c991368b185154fc045a8d8c5a87c07d74a7db9b619bf66a9e3`. Only actor composition/camera diagnostics changed. No resources, collision tiles, landings, source artwork or user map packages were removed/rewritten. The four owned probe packages were moved out of the mod maps folder afterward.

Next implementation must resolve both front corner continuity and visible authored rear stone, scoped away from approved Two Sides banks. Shared editor rendering and physical coast/landing regressions are still required before accepting a production change.
