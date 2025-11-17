- Group mutually exclusive promotions in the same PromotionPalette group so the UI treats them as a cluster.
- Rename any  `ProvidesPrerequisite@filler` blocks to `@upg` (and adjust `Prerequisite` names accordingly).
- Remove `ProvidesPrerequisite@upg` blocks whenever their `upg.*` prerequisite is unused in the faction file. Keeping dead prerequisites around breaks the guideline about real promotion data only.
- Reference upgrade prerequisites (`upg.*`) without a leading `~` (use `upg.channeler`, not `~upg.channeler`).

- Use `anypower` without a leading `~` when gating power availability; `~anypower` is invalid in commander trees.
