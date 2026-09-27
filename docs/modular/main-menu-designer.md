# Main-menu custom faction editor (first GDI slice)

## User workflow

Restart the locally built game and select **Eigene Fraktion** in the main menu.
The panel edits a named custom profile based on **GDI / Eagle**, keeping the
existing roster and adding one configurable tank.

- Set faction name and tank name.
- Select running gear (tracks / hover / GDI-only stationary platform).
- Select standard or boosted diesel and baseline or efficient generator.
- Select normal tank shells or an experimental HE package (complete existing
  120mmHEAT template, not a recolored ordinary shell).
- Hull, armor, main turret and cannon are visible but currently have only ONE
  supported choice each. This is NOT the full 75-component catalog in the UI.
- Price, raw HP, speed, tech, catalog points, mass and static power update live.
- **Speichern** persists the profile. **Speichern und Testspiel** saves it, freezes
  the current design into a private map snapshot and opens the normal skirmish
  lobby with that map selected. No external Python process is used by the game.
- Use the first player slot / start A. The configured tank, stock MTNK comparison,
  enemy target and manual Carryall are near that start.

The supported controls currently yield 24 combinations. Base tracked tank is
900 credits / 52000 HP / speed 82 / CP 0. With baseline motor/generator the hover
version is 1100 / 52000 / 79 / CP 0; stationary is 850 / 52000 / 0 / CP 0. HE adds
25 credits to these examples. These extra costs are experimental allocations.

Normal/hover custom tanks are additional GDI/Eagle vehicle factory choices on the
snapshot map. The compiler applies the highest selected tech tier as a production
prerequisite. The original GDI units remain available. Stationary vehicles are
preplaced and carryable ONLY for now; no fake stationary factory rollout is enabled.

The faction keeps internal ID `eagle`, production prerequisites and commander
rules. Its display name changes only in the snapshot map via `FactionCA@11`.
This avoids pretending that arbitrary new faction IDs already inherit all GDI
production/upgrade traits. Other Eagle players on that snapshot share the same
custom roster. Normal maps and ongoing matches are unaffected.

## Scope and remaining limitations

This first integration launches the known test terrain, NOT arbitrary selected
maps. Editing a profile does not silently inject units into every later ordinary
match. Multi-design rosters, other base factions, visual previews, arbitrary
weapon/trait composition, custom CP unlocks, and lobby design negotiation are
still future work. Hover and stationary use tracked-tank placeholder artwork.
The current terrain has no added dedicated water test course.

The full stock MTNK actor and weapon templates remain inherited, including
conditional legacy modifiers. The GDI roster is not independently reconstructed
from components. This is a narrow, usable editor/compiler path for one additional
tank rather than a claim that the entire faction compiler is complete.

## Persistence and match freeze

- Profile: `<OpenRA SupportDir>/Modular/custom-faction.json`.
- Explicit saves preserve the previous file as `.bak` and atomically replace the
  profile via a temporary file. A corrupt/unsupported profile is reported and is
  not rewritten merely by opening the editor.
- Compiled maps: `<OpenRA SupportDir>/maps/ca/modular/custom-<SHA256>.oramap`.
- Existing content-addressed maps are never overwritten. The map embeds the frozen
  profile and generated actor/weapon rules; changing the profile later cannot
  mutate an existing match, replay or saved snapshot.
- The mod registers this user-map folder and selects the generated UID through
  the existing local-server/skirmish workflow. Designer test sessions bypass the
  stock `skirmish.ca.yaml` restoration AND persistence: otherwise joining the lobby
  silently replaces the generated map with the last ordinary skirmish map. A fresh
  default bot is added instead. Normal skirmishes keep their original behavior.
  This isolation lasts for the test server's lifetime, including manual map changes.
  No installation directory writes
  and no local JSON reading by simulated actors.
- Normal map transfer/checksum machinery handles the generated snapshot if used
  with other clients. Multiplayer transfer and save/load have NOT been live-tested.
  This is not a new independent multiplayer faction-sync protocol.

## Implementation

- `OpenRA.Mods.CA/Modular/CustomFactionDesign.cs`: pure native validation, arithmetic,
  profile persistence and deterministic map compiler.
- `OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs`: menu binding, dropdowns,
  live statistics, profile loading and test-lobby launch.
- `mods/ca/chrome/custom-faction.yaml`, mainmenu/mod registrations.
- `mods/ca/modular/designer-catalog.json`: shipped, restricted component snapshot.
- `tools/modular/export_designer_catalog.py`: developer-only exporter from the
  offline catalog/prototype definitions, plus the explicitly priced HE test loadout.
- `Modular.Tests`: native compiler tests, including all 24 combinations against
  checked-in results from the independent Python calculator.

IDs/options and template bindings are deliberately allowlisted. Adding an ID to
JSON alone cannot silently enable unsupported behavior. Missing roles, invalid
names, unsupported faction, CP-bearing modules and unknown profile fields are
rejected. No per-design discounts or arbitrary YAML inputs.

## Validation

```sh
python tools/modular/export_designer_catalog.py
python -m unittest discover -s tools/modular -q
dotnet test Modular.Tests/Modular.Tests.csproj -c Release
dotnet build OpenRA.Mods.CA/OpenRA.Mods.CA.csproj -c Release
```

Verified: **63 Python tests**, **16 native tests**, successful CA Release build.
The extra regression tests cover test-session isolation and server-trait registration.
The first interactive user report exposed stock skirmish restoration overriding
an initially correct generated map; `CustomFactionSkirmishLogic` now prevents that.
An interactive retry with this correction is still pending.
An exported native HE sample was read by Engine Utility: 1687 rule-lint errors,
versus 1685 on the stock control. The two additional diagnostics repeat the stock
MTNK condition/palette errors for `modular.custom`; no invalid faction or missing
field errors appeared. This is NOT a clean full-mod lint pass.

Interactive user testing of the menu has begun. The corrected lobby handoff,
factory production, Carryall transport, hover/water, multiplayer transfer and
save/load still need confirmation in-game.
