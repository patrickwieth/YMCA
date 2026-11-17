# Two Sides: GDI versus Soviets menu battle

## Installed

`mods/ca/maps/two-sides-shellmap/` is a separate generated map, selected as the
current shellmap. No existing map terrain, actors or gameplay were overwritten.
The former test-map shellmap flags are backed up under
`art-source/two-sides-shellmap/previous-shellmaps/`; those maps remain available
with their other visibility flags retained. User-map folders were checked: the
new map is the only currently enabled shellmap.

Terrain is the actual Tactical Two Sides generator, seed1967, two teams/players,
96x96, resource patches with renewable generators, no tech buildings. The two
banks are connected by the generated central crossing. GDI (Talon, yellow) fights
Soviets (Russia, red).

## Behavior

- Both players use the existing normal-AI decision modules to build, repair,
  produce units and form attack squads. Buildings are genuinely constructed and
  follow the current faction artwork/footprints; later attacks are not fake
  scripted spawns.
- Small starting bases and one opening combat patrol per side avoid an empty menu.
- Cash is replenished to20000 every10 seconds so the background does not stall.
- Only the two initial construction yards and their two home guards are protected.
  Other structures can be destroyed and rebuilt. Victory conditions and AI
  support-power targeting are disabled only for this map.
- New base construction pauses at24 extant buildings per side; already queued
  placements can finish. It resumes after losses. Combat units retire after180
  seconds if they become stranded. Idle production has its own30-unit limit.
- A slow camera drift follows the crossing; world combat sounds are muted in the
  menu, without changing ordinary match sound settings.

Shellmaps have no client-backed combat players and no regular host admin. Normal
bot network orders therefore fail ownership/client validation. The map-only
`ShellmapBattleMonitor` trait drives the same bot modules through a local queue
that checks subject ownership and expands grouped orders. It refuses to create
these local bots outside `WorldType.Shellmap`; no multiplayer order validator was
weakened or modified.

## Verification

An isolated game instance with separate settings/logs and its own temporary mod
manifest ran the actual shellmap. It did not interact with an existing game.
Opening, mid-battle and later menu screenshots were reviewed. Both sides built
additional structures, produced units and scored kills in both a6000-tick run and
a subsequent3500-tick run of the final bounded-base version; both exited normally.
The final run reached GDI16 / Soviet24 extant buildings, with continuing production
and combat on both sides. No script errors or rejected client orders appeared.

`--check-two-sides-shellmap` validates the actual map/rules, factions, initial
footprint terrain/non-overlap, production exits and complete sprite loading.
`python tools/test_two_sides_shellmap.py` runs this preflight and layout checks.
The generic full-mod lint still reports existing unrelated prerequisite/condition
issues; it is not claimed clean. The only two new unsupported map fields found
while developing were removed.

## Reproduce / select / restore

- `python tools/prepare_two_sides_shellmap.py`: generate terrain if needed and
  assemble the map/rules. It leaves visibility0 until an installed runtime passes
  validation. It does not replace any existing map.
- `shellmap.cmd`: build the current code, validate/select this map, start the menu.
- `python tools/activate_two_sides_shellmap.py`: validate installed runtime before
  selecting the new shellmap and retiring old shellmap flags.
- `python tools/activate_two_sides_shellmap.py --restore`: restore previous flags
  without overwriting later terrain/actor edits.
- `tools/run_two_sides_shellmap_probe.py --ticks 3500 --wait`: isolated live test
  using `.art-validation/shellmap-runtime/bin`; see its build prerequisite.
  Environment variables `YMCA_SHELLMAP_REPORT` and `YMCA_SHELLMAP_TEST_TICKS` enable
  reporting/self-exit only in that test process. Normal menu play has neither.

The current CA DLL was installed locally after the successful tests. No commit,
push, release, deployment, or stopping of a user's game was performed for this task.
