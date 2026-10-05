# Shore fog, real two-client synchronization and expanded seeds

This follows the source-shore rollout. It records finite tests, not a blanket
claim that every visual combination, competitive matchup or GPU is certified.
Bridges were excluded from this batch; see the subsequent
[six-part bridge implementation and tests](rubberduck-bridges.md).

## Shore visibility lifecycle

`--rubberduck-authored-shore-test OUTPUT fog [shaded]` now tests five materials in
four directions. Each revealing source sits on land and checks both its land cell
and the adjacent water cell. These are actual `RevealsShroud` traits, not direct
writes into visibility state.

Evidence: `%TEMP%/ymca-shore-fog/accepted-{normal,shaded}`. Each session passed:

- 40 visible-and-explored samples at tick 100.
- 40 explored-but-no-longer-visible samples at tick 500 after all local revealing
  units were removed at tick 450.
- A distant cell remains unexplored/invisible before and after removal.
- Ten real land/water movement assertions and 95 mixed-water texture join checks.
- An actual framebuffer after source removal; explored terrain remains dim,
  unvisited terrain remains black.

Two earlier fixtures are rejected as lifecycle evidence. An invisible source with
an empty footprint needed `Type: CenterPosition`; its default footprint revealed
nothing. Removing the starting MCV also ended the first match before tick 500.
The diagnostic now disables conquest victory explicitly; ordinary rules are not
changed. Early movement screenshots alone were not accepted as a completed test.

The existing `occlusion-water` and `occlusion-water-shaded` fixtures were rerun with
native shores enabled. Each recorded 18 cargo/coast PASS checks and a framebuffer
with the foreground height-four roof over the water/ground transition. These are
selected occlusion cases, not all possible camera/contact combinations.

## Two real network clients

Generate a fixture from an ordinary production Mountain Valleys map:

```
--rubberduck-network-test MAP FIXTURE_DIRECTORY
powershell -File tools/test-rubberduck-network.ps1 -FixtureDirectory FIXTURE_DIRECTORY -OutputDirectory FRESH_OUTPUT_DIRECTORY
```

The PowerShell runner starts a separate dedicated server and two actual clients.
It uses isolated support directories, a shared content junction, a fresh output
folder, no online advertisement/NAT mapping, and a temporary diagnostic map. It
stops only its own processes and removes its temporary map in `finally`. User
settings are copied, not overwritten.

The diagnostic map marker permits auto-ready only on a non-advertised dedicated
server with exactly two validated loopback clients. Ordinary multiplayer and
skirmish maps do not take this branch. Startup uses the normal enclosing lobby
command's auto-start path; an initial test's extra `StartGame` call started twice
and was removed. That failed test is not synchronization evidence.

Final evidence: `%TEMP%/ymca-network-accepted` (runner output and both isolated
client logs/screenshots). On the unchanged 4P42 Mountain Valleys terrain with two
active human slots:

- Both clients completed **3000 simulation ticks**.
- Each client sent **18 Move orders** for its own tank through `World.IssueOrder`.
  The other client's simulation received these through the real network path.
- All **30 synchronized hash AND two-tank position snapshots** matched exactly.
- Both tanks actually moved between distinct positions.
- One game start, no crash or desync in the accepted run.

This is a two-minute Tactical loopback movement/synchronization smoke test. It is
not a long multiplayer battle, WAN latency/disconnect test, or complete validation
of Operational/Strategic multiplayer. The clients use the same Windows/Intel Arc
machine; this is not additional hardware coverage.

## Expanded seeds exposed a real generator defect

The new matrix uses all nine presets, 4/12 players and seeds 101/202: **36 exports**.
The original Archipelago 12P101 run failed because resource cell 62,30 was water.
The home-island blob's nominal radius did not guarantee that the complete radius-2
resource patch, offset seven cells inward, fitted inside its irregular boundary.

`IslandsGenerator` now checks the whole home patch before placing it and rejects
that candidate inside its existing deterministic full-layout retry loop. Final
island validation also rejects resource/generator cells on non-traversable terrain.
It does not trim resource patches, fill water, relax movement, or hide the error at
export. The failing case succeeds at attempt 1 with all 156 resource cells and
12 generators intact.

`--rubberduck-island-economy-test` permanently covers 4/12/16 players and seeds
42/43/101/202, generating each case twice. All twelve cases passed equal-sized
13-cell home patches, one generator per player, dry terrain and exact deterministic
terrain/features/home ownership/spawns. Existing Archipelago 4P42 `map.bin` remains
byte-identical to its earlier production baseline.

Evidence: `%TEMP%/ymca-expanded-seeds`, `/tmp/island-economy-regression.log`.
All 36 exports passed after the fix. The four new Mountain Valleys exports also
passed the closed-contour audit (180 valid contour tests / 543 corrupt rejections).
Running that closed-family-only command on unrelated presets is not a valid test;
the early mixed-directory invocation was corrected to contain only Mountain Valleys.

The conservative naval graph audit ran across all 36 maps. Its access/width reports
are in `ymca-expanded-seeds/naval-audit`. Component IDs are enumeration IDs, NOT
largest-sea ranks. Compare every substantial water body before interpreting a large
nearest-shore difference: Two Sides and Continents deliberately have several seas.
Graph reachability is not a substitute for actual ship timing or universal balance.

## Still not claimed

Remaining art-family/contact combinations, broad long multiplayer/WAN tests,
additional hardware and universal seed/matchup balance remain outside this evidence.
See the main terrain acceptance checklist for outstanding gates.
