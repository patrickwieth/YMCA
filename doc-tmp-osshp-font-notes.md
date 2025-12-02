# OS SHP Cameo Font Extraction Notes

- Source: OS SHP Builder 3.3x.r113/FormCameoGenerator.dfm -> ImageList1 Bitmap (20x12 glyphs, 80x228, 32bpp)
- Extracted asset: Game/mods/ca/uibits/osshp-font.png (used by ProductionIconButtonizer via OS SHP bitmap font renderer)
- Rendering: 1x scale, original per-pixel colors, 1px shadow, tight kerning (glyph width only), bottom-aligned in bar (base bar height 5px; auto +1 for single-line, +4 for two-line); line spacing ~0 (-6px per gap), text y-offset +9
- Current bar colors: default top (200/60/60/60 alpha), bottom (220/40/40/40 alpha); RA1/TD/TS use darker variants
- Outstanding: visual match vs. OS SHP cameo generator still needs user confirmation; banner darkness/position/kerning/line-break (wrap >13 chars into up to 2 lines at last space) are adjustable via ProductionIconButtonizer styles.

- 2025-11-24: Re-exported the glyph sheet by stripping the raw bitmap out of FormCameoGenerator.dfm and rebuilt osShp-font.png as a compact 4x10 grid ordered [0-9][A-Z][.].
