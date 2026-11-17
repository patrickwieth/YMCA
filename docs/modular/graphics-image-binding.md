# Compiled actor image binding

## Scout Drone startup crash

A compiled GDRN design crashed in `WithIdleOverlay` with:

```
Image `modular.custom.<design-id>` does not have any sequences defined.
```

`RenderSprites.Image` can be null in original resolved rules: the engine then uses the
actor's own name. Inheriting GDRN under a new actor ID does not preserve that fallback.
The previous `PreserveActorGraphics` branch omitted the image override entirely.

The compiler now always binds `RenderSprites.Image` explicitly. The 44 generated stock
assembly bindings use the **resolved image from stock-combat-baselines.json**, not simply
the actor name. This also preserves aliases such as `1TNK -> light_tank`. Other inherited
render properties, faction image dictionaries, palettes, overlays and complete weapon
packages remain unchanged. No graphics assets, numeric calibration or saved profiles change.

The headless check now resolves the generated actor YAML through the real inheritance loader,
then compares original/generated sprite images for every supported base faction, explicit
faction-image key and fallback, plus voxel images and idle-overlay counts. It previously only
checked original-name preview actors, which could not detect renamed-actor fallback failures.
All 89 bindings pass. This is not a GPU render or complete gameplay test.

Native regression tests assert the exact resolved sprite image for all 44 stock bindings and
check a frozen GDRN map with a generated design ID. Existing frozen maps are intentionally not
rewritten: restart the updated game and use **Faction overview > Play - test map** to compile
a new content-addressed map. Launching the old broken map will still use its old rules.
