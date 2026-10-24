# Migration oil islands and the Plains name

## Migration

Automatic dimensions grow by 32 cells per axis (4 players: 96→128, 8: 128→160, 16: 192→224). The wizard and CLI share the same sizing path. Explicit custom sizes are not silently enlarged; layouts that cannot fit safely reject/retry.

There is one small oil island in each gap between neighboring starting islands. Each contains exactly one or two oil derricks; symmetry-related islands receive equal counts. Entire building footprints must be dry. No resource fields or resource generators are added there. The central island retains its finite resources and exactly six oil derricks per player; the new small islands add their oil separately. Starting islands remain empty of economy.

**All Migration coasts are now open**, including player starts and the central island. The old isolated rock fragments and their physical blocked banks are not exported. This is removal of the cliff-bank policy, not invisible collision.

Island rasterization accounts for staggered native rows. The initial uncorrected circular raster left isolated tip cells and was rejected by the new native-connectivity validator. The final validator requires one central island, one independent home per player and one independent oil island per player, with no causeways. Small-island placement keeps a sea gap and retries the whole layout rather than dropping islands or filling water.

Validation (`%TEMP%/ymca-migration-islets`):
- `--migration-island-test`: 28 twice-generated physical exports, Tactical/Operational, 2/3/4/6/8/12/16 players, seeds 42/43; deterministic terrain/economy, enlarged automatic dimensions, isolated islands, one connected sea, no cliffs/bridges/generators, exported derrick footprints and corrupted-home-economy rejection.
- Terrain-only naval audit on all 28 maps: all 956 route pairs connected at width 1 and width 3; width 5 has 944 connected and 12 disconnected pairs. This does not claim universal five-cell-wide channels or real-client travel timings.
- Real client overview and oil-island close-up: `%TEMP%/ymca-hill-team-plateaus/migration-*.png`.

## Arabia is displayed as Plains

The preset button, dropdowns, generated map titles, utility progress output and new permanent export filenames use **Plains**. CLI `preset=plains` is accepted. `preset=arabia` and the internal `MapGenerationPreset.Arabia` identity remain valid for compatibility. Saved maps and their existing titles/files are not rewritten.

`--arabia-layout-test` keeps its old command name, but now compares the Plains and Arabia aliases across 12 twice-generated layouts and checks the new exported title and filename. Real wizard navigation confirms Plains is shown and Arabia is absent from the preset page (`%TEMP%/ymca-ui-probes/plains-name`).

These changes do not repair the still-open Continents/Archipelago rock-coast topology issue. New generation is required for the Migration changes.
