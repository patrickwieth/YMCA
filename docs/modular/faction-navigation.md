# Faction navigation and chassis silhouettes

## Entry points

- **Play**, first in the main menu, opens the saved-faction chooser. Only this chooser
  offers **New faction**. Pick a name and one of the existing native base factions.
- **My factions** opens the management list, with opening and confirmed deletion only.
- Clicking a faction opens **Faction overview**: name editing, level, used/available
  catalog points, catalog categories and vehicle rows (name, tech, points, price).
- **Edit** opens the existing spatial vehicle designer as a child. Its faction creation,
  loading, base switching and roster-management controls are hidden. Back returns to the
  overview; unsaved vehicle edits require confirmation. New vehicle drafts are not written
  to the library until Save, and cancellation leaves the catalog unchanged.
- The overview owns New vehicle, Copy, Remove and Add stock templates. Mutations are
  validated and saved through copies; removal requires confirmation. Unsaved name edits
  require Save name or explicit discard before navigating elsewhere.
- **Play - test map** uses the existing immutable map compiler and protected designer lobby.
  Ordinary Singleplayer/Multiplayer remain available. This does not enable custom factions
  on arbitrary maps or multiplayer servers.

## Identity and persistence

`Modular/Factions/faction-*.json` remains the library. Existing hashed-name files are readable;
new entries use random identities. The file path, not its display name, identifies a faction.
Renaming updates that same file, retaining `.bak`, and never silently overwrites another faction.
Names remain case-insensitively unique after trimming. Original vehicle IDs are retained.

Deletion moves the JSON into `Modular/Factions/Deleted/` after the confirmation prompt.
The previous `.bak` is also retained, but neither backups nor archived entries appear in the list.
Existing compiled maps and the old active snapshot are not altered. A pre-library active profile
is imported only when the library directory does not yet exist; it cannot resurrect deleted entries.
Unreadable entries stay visible with opening disabled and deletion available. A corrupt legacy
active snapshot is preserved and reported without blocking creation of new factions.

Schema 2 now has optional `Level`, default **50** for existing files and new balancing factions.
Valid range is **1–100**; catalog budget equals level, and each design consumes its calculated
component points, not one vehicle slot. Numeric component calibration is unchanged. The separate
prototype ceiling of **16 vehicle designs** remains explicit. Level progression/XP is not implemented.
Infantry, Ships, Aircraft and Buildings are disabled categories, not runtime-admitted content.
The tier-grouped custom Commander Tree is still pending; no stock tree is replaced by this change.

## Lobby identity

On a frozen custom-faction map, matching faction rows show **Side: Custom** and the frozen
faction's exact name under **Faction**. Both columns use the original base sub-faction flag;
no nonexistent `games/Custom` icon is requested. The native faction ID, stock dropdown choices,
slot locks, prerequisites and runtime behavior are unchanged. Long names are truncated to the
existing column width and shown in full in the editable faction button's tooltip.

Identity comes from the map's `custom-faction.json`, not the mutable local faction library.
Legacy schema-1 snapshots are supported. Non-custom maps and nonmatching factions retain stock
labels. On map changes the metadata cache is reset and normal row setup restores the original
labels, flags and tooltips before applying any custom presentation. Spectators are unaffected.
This changes lobby presentation only; it does not add arbitrary-map custom-faction selection.

## Silhouettes

The spatial editor chooses presentation from the bound native actor, never from translated names
or shared running-gear labels. Initial explicit styles cover:

- Tracked tanks (fallback).
- HMMV, BGGY, JEEP: wheeled light body/cabin and smaller weapon mount.
- BIKE: two-wheel frame and light mount.
- BTR, APC2, IFV, KATY, V3RL, HQ7, HTK5: multi-wheel body.
- TITN: upright heavy walker with a 4×6 chassis grid and 5×4 turret grid.
- Juggernaut, Mammoth Mk II, XO, GUNW: articulated walker legs (Mk II's four-legged fixed platform is the next revision).
- MDRN: integrated hover-drone silhouette.
- GDRN: miniature tracked silhouette and Mini-class turret socket.
- TPOD, Hexapod: additional visible support leg.
- Other explicitly native-hover bindings: hover pods and no tracks.

These are class silhouettes, not individual stock sprite reproductions. The right-hand original
rotating sprite/voxel preview remains authoritative for original artwork. All cells are now 96×96 UI pixels, with a larger, pannable central canvas,
including hit testing, held snapping and installed blocks. Body and separately rendered turrets
counter-rotate. See [the latest layout refinements](turret-layout-refinements.md) for the taller
window, explicit turret classes, and the pending fixed weapon socket revision.
These grids are not physical-volume calibration; explicit native compatibility and stock combat
values remain unchanged.

## Validation and live review

Native tests cover identity-preserving rename/backups, name conflicts, path confinement, deletion,
non-resurrection, optional-level migration, budget enforcement, silhouette selection and unchanged
layout budgets. UI source checks cover entry points and session routing. These do not prove live
widget/graphics correctness.

Manual checks: create from Play; cancel creation; reopen from My factions; cancel/confirm deletion;
rename and reopen without duplicates; attempt a conflicting name; edit/save a vehicle and return;
cancel a new vehicle draft; test-game lobby; switch MTNK/HMMV/TITN/TPOD and move/rotate/drop blocks
in each grid, including the upright Titan layout. Verify original preview fidelity.
