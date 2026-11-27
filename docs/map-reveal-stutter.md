# Full-map reveal stutter — Game development

Reproduced the user's report on Glitters using an isolated native client: normal fog, then full debug visibility at tick 150. Also exercised the same PassiveVisibility source used by RevealsMap (satellite scan). This is a renderer test, not an automated click through the satellite support-power UI.

## Findings and changes

- The reveal call itself takes under 0.2 ms, while the following frame previously stalled for about 2.6 seconds.
- Revealing thousands of resource cells made TiberiumTerrainLight create many overlapping light sources at once. Every insertion immediately notified every affected cell; several terrain layers repeatedly recalculated the same corner tints while the light set was still changing.
- `terrain-lighting-batch.patch` coalesces notifications by cell and caches identical position tints only during dispatch of a completed batch. Mutating a light clears that transient cache. Nested batches flush only at the outer scope; ordinary non-batched callers retain immediate notifications.
- Tiberium glow updates have a render-local budget (128 updates / 2 ms before notification dispatch). Visibility and simulation are NOT deferred. Only the cosmetic glow may settle over subsequent frames. Disposal also batches light removal.
- Flat-map resource rendering now bypasses higher-terrain pixel-mask allocation. Height/tile revision invalidates this fast path; raised terrain retains the clipping code.
- Hidden resources are still not queried for current density. Native tests verify that hidden depletion keeps remembered light and that subsequent reveal removes the depleted light. Nested batch notifications, unique affected cells, tint preservation and light removal are also checked natively.

## Measured evidence

Saved under `art-source/handcrafted/validation/`:

- `reveal-before.json`: debug full reveal, maximum subsequent frame interval **2565.7 ms**.
- `reveal-debug-after.json`: same original benchmark package, maximum **100.0 ms**, maximum lighting update **2.35 ms**, no pending lights, fog-memory test passed.
- `reveal-satellite-after.json`: final satellite-equivalent visibility source, maximum **172.8 ms**; fog-memory and batch checks passed.

These are local wall-clock render intervals (including the frame cap and OS scheduling), not multiplayer latency or universal hardware guarantees. The multi-second stall is greatly reduced; a roughly 0.1–0.17 second peak still exists, so this is NOT evidence of perfectly hitch-free reveal.

`MapRevealPerformanceProbe` is opt-in only. The exporter accepts `reveal` / `reveal-satellite` in a separate review output directory; never install those packages. Production maps contain neither this probe nor the calibration camera.

All work is in Game. No server restart, user-game termination, release, push, or Game-master change was performed for these refinements.
