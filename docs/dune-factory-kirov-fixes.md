# Dune boundary, Nod exit and Kirov bombs — local Game fixes

Implemented locally in Game; no commit, push, release, server change or footprint edit.

## Dune Patrol

The raised visual roof now continues from each existing top-edge cliff column to the logical northern map boundary. This fills the previous dips behind the wall without moving its playable-facing contour. Tile-based roof occlusion, logical boundary clipping and the source-derived closed side corners also apply to Dune. Gameplay terrain overrides, elevations, resources, starts and original actors are unchanged.

Production UID: `5577395023c6f50282673e70fa16f70a954ea49d`.
The prior `25481cfec608bf5758fe5f093efb9f8f500c713c` package is preserved byte-exactly and indexed as Unknown, not selectable. The original remains in history/before-coast-and-space-refinement.

Native overview and close-up inspected. Ground movement, destroyer movement on the small pond, and all fourteen construction-yard placement checks passed. The package regression checks the entire logical top edge is roof tile 3992 while retaining the original gameplay data.

## Kirovs: not caused by the shellmap

KirovBomb inherits ^ShellArtillery's **MinRange 4c0** but overrides maximum Range to **1c0**. FlyAttack rejects this impossible range interval. The shared artillery minimum dates to 2022; Kirov's artillery inheritance dates to 2025. This predates the current shellmap.

Liberty's `liberty-weapons.yaml` already corrected the minimum to zero **locally**, so cinematic bombers worked while normal ones did not. The normal KirovBomb now explicitly declares `MinRange: 0c0`, inherited by its Tesla, cluster and nuclear variants. Damage, reload, speed, altitude and maximum range are unchanged. Artillery keeps its existing four-cell minimum; cinematic durability/speed remain local to LIBERTY.KIROV.

Native A/B: Russian, North Korean, Iraqi and default bombs all dealt zero damage and abandoned attack before the fix. Afterward all four approached and destroyed their separate ground targets. The final probe asserts actual damage, not just resolved weapon fields.

## Nod weapons factory

Resolved, unchanged building geometry is 4x3, footprint offset -1,-1, footprint `xxxx x+xx =+==`. The inherited GDI exit (0,2) lies beyond Nod's protected placement area. A legal placement could therefore put its only exit on an existing tree. This was reproduced on Dune with a t09 at the exit: production returned false for buggy, bike and light tank despite not being paused/disabled. GDI's comparison factory produced normally.

Nod now overrides only Exit@1.ExitCell to **0,1**, the existing passable front transit cell. SpawnOffset, building dimensions, footprint, center/art alignment and GDI exit are untouched. The formerly blocked Nod factory now produces all three vehicles and they detour around the tree to the rally point.

The final native probe also tests a second Nod factory WITHOUT a rally point: all three vehicles are produced and clear the exit. Nine vehicles total (two Nod cases plus GDI control) must reach their expected destination/exit neighborhood. No real map tree was deleted to pass the test.

## Regression/evidence

- `python tools/test_kirov_factory_regressions.py`: resolved four-bomb range, unchanged artillery minimum, protected Nod exit, unchanged footprint/GDI exit, shellmap scoping.
- `python tools/test_handcrafted_packages.py`: Dune visual/gameplay separation and no diagnostic traits in production.
- Review-only trait: `FactoryAirshipProbe`. Its final Passed flag requires all four attacks, nine successful production calls and all nine vehicles reaching their rally/exit neighborhoods.
- Generate a Dune `review` package with the normal handcrafted exporter, then run `python tools/prepare_factory_airship_probe.py REVIEW.oramap OUTPUT.oramap`; launch with test-handcrafted-map-runtime.ps1 -WaitForMovement. Never install the probe package.
- Evidence: art-source/handcrafted/validation/factory-kirov-{before,fixed}.json and dune-patrol-{runtime.json,overview.png,wall-detail.png}.
- The historical before report's Passed flag meant diagnostic completion only; its zero damage / failed Nod production fields are the baseline evidence. Final assertions were added afterward.
