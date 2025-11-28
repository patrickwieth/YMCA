- Group mutually exclusive promotions in the same PromotionPalette group so the UI treats them as a cluster.
- Rename any  `ProvidesPrerequisite@filler` blocks to `@upg` (and adjust `Prerequisite` names accordingly).
- Remove `ProvidesPrerequisite@upg` blocks whenever their `upg.*` prerequisite is unused in the faction file. Keeping dead prerequisites around breaks the guideline about real promotion data only.
- Reference upgrade prerequisites (`upg.*`) without a leading `~` (use `upg.channeler`, not `~upg.channeler`).

- Use `anypower` without a leading `~` when gating power availability; `~anypower` is invalid in commander trees.

- Keep commander tree BuildPaletteOrder values within the shared low ranges (3-21) so every faction lines up with the Scrin overlay layout instead of resurrecting legacy 200+ slots.
- Store all Buildable metadata (IconPalette, BuildPaletteOrder, Prerequisites, Description, etc.) inside the Buildable block; never duplicate these fields under PromotionPalette, TooltipExtras, or other sections.
- PromotionPalette sections should only declare palette/visual data (Group/Name/Column/Types); no gameplay data or tooltip text belongs there.
- Every promotion needs a TooltipExtras block that lists Strengths/Weaknesses (and optional Attributes), and those lines must stay under TooltipExtras rather than leaking into other traits.
- When a promotion unlocks or modifies a real unit, include a RenderSprites block so the commander-tree icon references that unit's sequence instead of falling back to placeholders.
