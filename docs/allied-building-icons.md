# Allied building cameo set

Extends the user-approved Power Plant / Fusion Plant treatment to the rest of the
Allied building palette: source-art close-ups, 96×72 logical cells, 192×144 RGBA
textures, dark blue backdrop and the existing OsShp pixel lettering.
**The two approved power-plant PNGs were not regenerated or modified.**

## Coverage

Allied production, defenses, France's Grand Cannon, England's Camo Pillbox and all
four fake buildings are covered. Fake and camouflaged variants retain distinct
captions. Construction Yard/Nukular display variants share the appropriate cameo.
The disabled legacy Silo/Turret also have matching icons; their `~disabled`
prerequisites remain unchanged. Non-buildable map-editor walls are not production
palette entries and were not changed.

Oil Pump, Oil Platform and Swiss Bank use faction-specific sequence aliases only
for England, France and Germany. Other factions retain their previous icons. The
Allied Construction Yard gets its own image alias rather than modifying `fact/icon`,
which is inherited by Soviet sequences. No building art, dimensions, health, cost,
power, prerequisites, weapons, income or construction sequences were changed.
The user's oil-platform waterline remains untouched.

## Files and regeneration

- `mods/ca/bits/allies/icons/`: runtime cameos.
- `mods/ca/sequences/allied-building-icons.yaml`: icon-only bindings/captions.
- `mods/ca/rules/allied-building-icons.yaml`: isolated image aliases and Camo icon key.
- `tools/prepare_allied_building_icons.py`: reproduces this set, never the approved
  power icons or source building PNGs.
- `art-source/allied-building-icons/`: editing masters, crop/source hashes, legacy
  native sprite exports and review images.

Most sources are the actual runtime building PNGs. The flak icon composites a
native turret facing with its original pedestal. Grand Cannon, Swiss Bank and the
disabled classic Turret/Silo use native SHP frames; these retain their original
limited detail, not invented high-resolution mechanisms. Export settings are in
`source-exports.json`. Their player-color indices use the same blend as
`OverlayPlayerColorPalette`, with fixed Allied blue for the cameo. Non-remap pixels
and alpha are preserved. The Oil Pump uses a native animation frame.

## Safety and validation

Duplicate inheritance keys in HBOX and the shared income-building definitions
were given distinct suffixes so the new MiniYaml override blocks can merge. Parents
and ordering are unchanged. Resolved-rule snapshots verify identical gameplay and
non-icon sprite sequences for every audited buildable building and faction.
Unrelated icon bindings are compared too.

- `--audit-building-icons OUTPUT.json`: engine-resolved snapshots.
- `tools/test_allied_building_icons.py`: coverage, approved pixel hashes, source
  preservation, remap limits and before/after gameplay/body/other-faction checks.
- `tools/test_allied_power_icons.py`: original approved power-icon checks.
- `tools/prepare_allied_icon_gallery.py SOURCE DEST [--faction england|france|germany]`:
  isolated full-menu fixture. `YMCA_ALLIED_ICON_REVIEW=1` enables native validation;
  runner option `-WaitForResultFile allied-icon-review.json` waits for completion.
- The gallery alone supplies tech/country prerequisites to display every variant;
  these fixture-only unlocks are not in production rules or the ordinary test map.
- Native review covers all resolved cameos in Building/Defense menus, including
  scroll pages at 1×, 1.25× and 1.5×. The diagnostic waits for complete asynchronous
  PNG writes before reporting success. Screenshots/results are under `validation/`.

Local development only: no release, push or deployment.
