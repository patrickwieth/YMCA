# Remaining supplied China / Scrin buildings — locally integrated

Worktree: Game / isometric. Supplied PNGs are referenced directly, without recoloring,
rewriting originals or redirecting them through derivative folders. All newly bound
files are RGBA; the earlier eight indexed Scrin sources retain their own palettes.

## Inventory

| Actor | Direct original in bits/.../buildings | Scale | Bottom offset |
|---|---|---:|---:|
| chpower | china/power plant.png | 1 | 0,10 |
| chpower2 | china/advanced power plant.png | 1 | 0,14 |
| chbarracks | china/barracks.png | 1 | 0,14 |
| chcommand, chcommand.Nukular | china/command center.png | 1.5 | 0,12 |
| chsupply | china/supply depot.png | 1.25 | 0,15 |
| chairport | china/airfield.png | 1.5 | 0,20 |
| chpropaganda | china/propaganda center.png | 1.25 | 0,13 |
| chinternetcenter | china/internet center.png | 1.25 | 0,12 |
| chweap | china/war factory.png | 1 | 0,2 |
| chsilo | china/nuke silo.png | 1.25 | 0,30 |
| CHSYRD | china/naval yard.png | 1.5 | 0,19 |
| chpropagandatower, chpropagandatower.pdl | china/propaganda tower.png | 1 | 0,-1 |
| SREP | scrin/repair bay.png | 1.25 | 0,-4 |
| SCOL, SCOL.Temp | scrin/storm column.png | 0.65 | 0,-18 |
| RIFT | scrin/rift generator.png | 1.25 | 0,-28 |
| MANI | scrin/field manipulator.png | 1 | 0,-34 |

War Factory arrived during implementation and is included. It is384×384; Speaker
Tower is128×128 and the other building images are256×256. The144×50 `china/radar.png` is a single transparent sheet
with three roof attachments, not an animated three-frame strip. It is referenced
whole on the Command Center at source top-left45⅔,29⅓, under the original radar-upgrade
condition. Its scale follows the building. The radar plug's gameplay is untouched.

Previous supplied Scrin buildings, Shard/Plasma and Chinese Gatling remain installed.
All currently supplied China building PNGs now have bindings. Assets for the remaining
China bunker.png and fortified bunker.png have now been supplied and directly bound,
including AI/advanced-infantry variants; existing Soviet2x2 bibs and gameplay retained.
See `china-bunkers/` for prior bindings and placement review. This is not a claim that
every faction actor was redrawn.

Latest placement feedback is applied: weapons factory16px right; Speaker Tower/PDL8px
up with selection/effect origins following; Internet Center bib enlarged from3x3 to4x4.
See `china-placement-followup/README.md` for validation, previews, the PDL inheritance
fix and the separate outstanding reactor-art/historical-hash checks.

## Placement and state handling

- Per the latest user request, ALL Chinese land buildings retain the same native
  Soviet bib sequences used by the Soviet faction, including the supplied PNGs and
  Nukular/PDL variants. Sizes1×1/2×2/3×3/4×4 follow the existing China mappings.
  Supplied pixels, their built-in floors, actor placement and muzzle positions remain
  unchanged. The water-only Naval Yard has no land bib, matching the Soviet yard.
  Legacy Scrin bibs remain removed on the three supplied replacements.
- Selection height and ground envelopes are fitted to actual nontransparent pixels.
  Existing Building dimensions, footprints, LocalCenterOffset, exits, spawn points,
  docking/reservations and production remain byte-equivalent in resolved gameplay.
- SCOL remains1×1, including the temporary summon. Its five discharge positions now
  sit on the supplied upper core/shoulder nodes and two lower cyan nodes, rather than
  old SHP heights. Weapon, targeting, cadence, conditions and storm boosts are retained.
- Both reactors retain their original animated smoke. Live feedback exposed the
  inherited terrain-dependent placelinesegment2 palette as incorrect: the overlays
  now use the fixed RA `effect` palette, with sequence Alpha0.8 and Scale2.
  All15 source frames were exported/reviewed with the matching temperat.pal: neutral
  white/grey steam, not terrain colors. Raw engine-loaded smoke pixels determine its
  unchanged base anchor (-9,-14); emitters are relocated
  to the supplied cooling-tower mouths, including both advanced-reactor towers.
- Repair Bay's idle/damaged AND active/damaged-active states use the new file.
  WithResupplyAnimation/RepairsUnits remain intact. Existing negative repair-pad depth
  is preserved so docked units are not hidden behind the whole single-image pad.
- Rift's active/damaged-active states also use the new file. The active overlay is a
  subtle13-frame additive source-image pulse (alpha0..0.18..0) instead of showing the
  obsolete SHP silhouette. Support power, readiness and active condition are unchanged.
- Original icons, construction and EMP/temporal/mind-control effects remain inherited.
  Damaged and repair-active art currently reuse the supplied static picture. No separate
  damage sprites, moving repair mechanisms, opening factory doors or animated radar
  were supplied or invented. Actual repair/production functionality remains unchanged.
- The duplicate plain inheritance key on chcommand.Nukular was renamed @NUKULAR,
  as previously done for SFAC.Nukular, so visual overrides can merge safely. The
  resolved Nukular gameplay is unchanged.

## Runtime files / reproduction

- mods/ca/rules/new-faction-building-art.yaml
- mods/ca/sequences/new-faction-buildings.yaml
- tools/art/bind_new_faction_buildings.py
- OpenRA.Mods.CA/UtilityCommands/CheckNewFactionBuildingsCommand.cs
- tools/test_new_faction_buildings.py

The generator reads `rules-before.json`, the actual-engine smoke registration and
supplied originals. `binding.json` records placement and source SHA256 values.
`source-hashes.json` also protects existing China/Scrin PNGs, Nod conyard.png and
original testmap1.oramap. Originals remained byte-identical. The additional War Factory
hash was captured on arrival, before binding it.

`placement-review.png` is a native-scale source composition with selection-ground
diamonds; Command Center includes its radar upgrade, Storm Column shows emitter markers.
It was inspected, including the late War Factory. This is NOT a live screenshot.

## Added Nuke Silo, Naval Yard and Speaker Tower

The three subsequently supplied originals are now bound, including the Speaker Tower
PDL promotion. Tower remains1×1; both repair and point-defense origins sit at its upper
mast. The PDL variant had an explicit legacy repair offset, so both its armaments are
registered explicitly rather than relying only on parent inheritance. Propaganda,
repair, PDL weapon/targeting/ammo/power and promotion behavior are unchanged.

Nuke Silo's launch origin is registered at source pixel116,148 in the open silo. Only
NukePower.SpawnOffset changes; missile, trail, launch delay, charge time, cost, damage
and support-power behavior remain unchanged. The supplied opening is static: no hatch
animation was provided or added.

CHSYRD now uses its own naval yard PNG, superseding the earlier shared Soviet image.
SPEN and SYRD are untouched. Existing Water terrain requirement,3×3 footprint, four
production exits, repairs and ship/submarine/boat production remain unchanged. The
new sequence inherits spen.soviet only to preserve existing UI/construction/effects;
all body states explicitly point to the Chinese original. Disposable map validation
now requires the separate Chinese image and still verifies water under both yards.

`silo-yard-tower-placement.png` was inspected at native placement/scale (composed
preview, not a live screenshot). Build, actual engine PNG/sequence/dependency and
launch/repair/PDL projection checks, source/gameplay regressions and the disposable
water/footprint test pass. Actual missile launch, ship departure and PDL firing still
need live review after restart. No original PNG was edited; no game was stopped.

## Latest Soviet-bib / one-pixel radar follow-up

The inherited CHINABIB overlays are restored on supplied Chinese land buildings.
The generator no longer emits their removal. Native Soviet PNGs/scale/ground depth
and construction gating are reused directly; see soviet-bibs-review.png. The radar
moves exactly one SCREEN pixel right and one up from the preceding position, requiring
2/3 source-pixel offsets at Scale1.5. Runtime Offset is now -10.333333,-125.666667.
No building or effect emitter moves with that radar-only correction.

Build and actual engine bib/radar loading passed. During the subsequent regression
run, the supplied Scrin rift generator.png disappeared and identical bytes appeared
as tech center.png. Source/selection tests then failed on the missing Rift path. This
external rename was not undone or silently rebound; clarification of the intended
Scrin actor mapping was requested. This blocker was subsequently resolved by the user's explicit Tech Center assignment
and newly supplied Rift Generator; see the following update.

## Scrin replacements and global wall-building removal

The user clarified that tech center.png belongs to SCRT, and supplied new Field
Manipulator and Rift Generator images. SCRT now uses the RGBA tech center.png directly
(Scale1, Bottom0,-56), in the original supplied-Scrin layer. Foundry remains on disk
but has no runtime rule/sequence references. MANI uses field manipulator.png and
RIFT uses the newly supplied rift generator.png, with recalculated placement above.
All idle/damaged/active states retain the new art; Rift's source-image pulse is retained.
Existing support powers, charge times, costs, footprints and prerequisites are unchanged.
See scrin-tech-mani-rift-review.png (inspected native-scale composition, not a live view).

The pre-replacement hash manifest is preserved in source-hashes-before-scrin-replacement.json;
new supplied-file hashes are recorded without modifying the images. Source/selection,
gameplay, old palette, historical China/Scrin and disposable-map tests now pass again.
Engine checks were built/run in isolation because OpenRA was running; it was not stopped.
Runtime changes are YAML/PNG bindings and do not require new gameplay engine code.

At the user's request, Buildable is removed from SBAG, FENC, BRIK, CHAIN, LASP and SWAL.
LASP explicitly removes its inherited Buildable trait too. CYCL, BARB, WOOD and LASF
were already nonbuildable. Thus sandbags, wire/chain/concrete/biometal walls and laser
fence posts are unavailable to every faction and AI, not merely hidden by one faction's
prerequisites. No AI build lists referenced these actors. Existing map/editor walls,
health, blocking, regeneration and laser-fence links retain all other resolved traits.
The Scrin wall remains a legacy display object in the disposable building test, not
an available build option. `test_no_buildable_walls.py` checks the full before/after
traits; the engine checker scans all resolved LineBuild actors for remaining Buildable.

No commit, push, release or deployment. Live review after restart remains pending.

## Earlier smoke / radar live-feedback correction

Radar X is unchanged. Only source Y moves38 ->30 (12 screen pixels upward at Scale1.5),
including chcommand.Nukular. The composed close-up was inspected for roof contact;
see smoke-fix/radar-before-after.png. Original artwork is unchanged. The roof fixtures
now meet the existing roof instead of sitting down inside it. Smoke uses Scale2 and
fixed effect colors as described above; neither building size nor emitter location moves.
Updated regressions and isolated engine validation pass; live confirmation after restart
remains pending. No running game was stopped.

## Validation and limits

Normal local build and `--check-new-faction-buildings` pass: real source RGBA/alpha
bytes, actor dependencies, resolved filenames/states, extras, icons/construction,
variants, reactor mouth positions and five Storm Column discharge points.
The reactor's smoke anchor was measured from all15 actual source frames, not guessed.
Initial Rift alpha syntax was corrected to the engine's explicit per-frame Alpha array.

Source/selection/gameplay/variant tests, historical China/Scrin regressions, earlier
Scrin supplied-art tests and disposable building-test map checks pass. The map checker
now requires the shared Soviet bib on all Chinese land actors, including supplied art.
Earlier Scrin palette checks and Chinese Gatling engine checks also pass.

Live GPU appearance, repair-pad vehicle layering, factory drive-out and aircraft
landing remain to be reviewed after restart. These checks are not a live production
or docking test. Existing test display slots automatically use the new actor images;
the authoritative map is unchanged. No running game/Blender was stopped, no commit,
push, release or deployment was performed.
