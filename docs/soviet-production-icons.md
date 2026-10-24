# Soviet helipad/silo production icon regression

The client crash on `hpad.soviet` was a missing image alias, not a terrain
failure. Commit `7f5aeadf` introduced `HPAD.Soviet` and `SILO.Soviet` without
explicit RenderSprites images. Their parents have no explicit Image either,
so resolution falls back to the derived actor names. No sequence groups exist
under those names.

Both Soviet variants now explicitly reuse the existing `hpad` / `silo` images.
This fixes production icons and placed-building animations without duplicating
art, changing prerequisites, or hiding the exception in the production palette.

Validation:

- Release build passes.
- `--check-buildable-images` passes 1007 resolved image/icon references,
  including faction overrides. It checks actors associated with actual
  production queues, excluding explicit `~disabled` proxies; internal support
  actors and prerequisite-only pseudo actors are not production palette items.
  This is reference validation, not an asset-decoding or map-override audit.
- Opt-in `ProductionIconProbe` in an isolated Russia fixture loads icon, idle,
  damaged-idle, make and active/stages sprites and switches the actual Building
  and Defense production palettes. Both PASS, with placed helipad/silo visible.
- Evidence: `%TEMP%/ymca-soviet-icons/ingame.png`, runtime-results pointer and
  saved fixture; `%TEMP%/ymca-soviet-icons-{build,test,client}.log`.
- Early fixture failures (invalid faction alias, non-YAML actor initializer and
  diagnostic log path) were corrected; only the run with both palette PASS lines
  is acceptance evidence.

[Client capture](images/soviet-production-icons.jpg).

Restart the client to reload rules. Existing generated maps use the shared rules
and do not require regeneration for this fix. No engine patch is involved.
