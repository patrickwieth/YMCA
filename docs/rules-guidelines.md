# rules.yaml Guidelines

These conventions apply to every `mods/*/rules/**/*.yaml` actor file so gameplay data stays consistent across factions and easier to review.

- **Indent with tabs, never spaces.** Top-level actors/prototypes are separated by a single blank line and start with `actor_name:` or `^Template:` so diffs stay clean.
- **Keep Inherits blocks at the top.** List shared templates (`^BigVehicle`, `^VoxelTurretedTank`, …) before actor-specific traits and keep tagged inherits (`Inherits@Foo`) immediately below the main `Inherits` list.
- **Buildable data lives inside `Buildable`.** Always put `Queue`, `BuildPaletteOrder`, `IconPalette`, `Prerequisites`, `Description`, and other build metadata underneath `Buildable:` and nowhere else (never under `PromotionPalette`, `Tooltip`, etc.).
- **PromotionPalette/Render sections only carry visuals.** Palette `Group/Name/Column/Types` or `RenderSprites/RenderVoxels` settings belong in their respective sections; gameplay gates, descriptions, or tooltip text do not.
- **Every buildable actor needs `Tooltip` + `TooltipExtras`.** Provide a `Tooltip` block with `Name` (`GenericName` when applicable) and add a `TooltipExtras` section that lists at least `Strengths` and `Weaknesses` (plus `Attributes` when relevant) so commander-tree and production overlays have data to render.
- **Order high-level traits consistently.** Preferred order after `Inherits` is: visuals (`RenderVoxels`, `RenderSprites`), buildability (`Buildable`, `Valued`, `Tooltip`, `TooltipExtras`), core stats (`Health`, `Armor`, `Mobile`), weapons (`Turreted`, `Armament`, `Attack*`), overlays/FX (`WithProductionIconOverlay`, `WithMuzzleOverlay`), death/upgrade traits, and finally any faction-specific custom traits. Stick to this order when adding new traits to reduce merge conflicts.
- **Reuse templates via `^` prototypes rather than copy/paste.** When multiple actors share trait stacks, extract them into a `^Template` so fixes propagate automatically.
- **Prerequisite hygiene.** Reference upgrade prerequisites (`upg.*`) without a leading `~`, keep negated prereqs (`!foo`) next to the positive ones they gate, and ensure every `ProvidesPrerequisite@tag` you add has at least one consumer in the same faction.
- **Category gates stay explicit. Use the positive category prerequisite before the negated variant (e.g., `aircraft, ~!eagle`) instead of shorthand like `~!aircraft.eagle`, so readers and tooling see both halves clearly.
- **Trait removal uses the `-TraitName` syntax.** When removing inherited traits (e.g., `-Crushable`), place the removal near related mobility/armor traits so reviewers immediately see intentional deletions.
- **Comments and TODOs live above the actor/trait they describe.** Use `#` comments sparingly and prefer documenting larger efforts in `/docs` when you need more than a sentence.
- **Validate after edits.** Whenever you touch `rules.yaml` files, run the relevant in-game test (e.g., `tools/test-game.cmd` or faction-specific skirmish) to confirm prerequisites, overlays, and palettes still resolve.
