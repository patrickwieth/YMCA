"""Render the agreed comparison from native generator output and recovered originals."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LABELS = {'MountainValleys': 'Mountain Valleys', 'TwoSides': 'Two Sides (generated)',
          'HillFortress': 'Hill Fortress', 'BattleNexus': 'Battle Nexus', 'Arabia': 'Plains',
          'WaterBasins': 'Water Basins', 'Continents': 'Continents',
          'Archipelago': 'Archipelago', 'Migration': 'Migration'}


def report():
    source = ROOT / 'art-source/handcrafted'
    generated = json.loads((source / 'generator-sizes.json').read_text(encoding='utf-8'))
    originals = json.loads((source / 'originals/manifest.json').read_text(encoding='utf-8'))
    rows = {(r['Preset'], r['Players']): r for r in generated['Rows']}
    lines = ['# Handcrafted Maps: recovered originals and size baseline', '',
        'Status: **three playable remakes implemented, natively tested and installed locally under Handcrafted Maps; not published**.',
        'See [native geometry validation and remaining release gates](handcrafted-layout-conversion.md).', '',
        'Approved targets: All that Glitters – Barren, Dune Patrol, and the original Two Sides/Naval.',
        'Category: **Handcrafted Maps**, separate from generated archetypes. Generator sizes and live',
        'tournament configuration remain unchanged. Remakes preserve original logical terrain, starts',
        'and resource records; native rendering uses the new tileset and current resource policy.', '',
        '## Current generated tournament maps', '',
        'Measured through the native `--map-size-report` utility, using the same Tactical / teams=2 /',
        'size=auto option parser as `--generate-tournament-map`. These are playable **bounds rectangles**',
        'in map coordinates, not traversable land, resource area, or walking distances. Water/cliffs',
        'reduce usable area. Hill Fortress/Nexus add non-playable backing outside these bounds.', '',
        '| Archetype | 2 players | Rectangle cells | 4 players | Rectangle cells |',
        '|---|---:|---:|---:|---:|']
    for preset, label in LABELS.items():
        a, b = rows[preset, 2], rows[preset, 4]
        lines.append(f"| {label} | {a['PlayableWidth']}×{a['PlayableHeight']} | {a['RectangleCells']:,} | {b['PlayableWidth']}×{b['PlayableHeight']} | {b['RectangleCells']:,} |")
    lines += ['', '## Original handcrafted references', '',
        '| Original | Stored MapSize (including border) | Playable bounds | Rectangle cells | Existing start slots |',
        '|---|---:|---:|---:|---:|']
    for m in originals['maps']:
        w, h = m['stored_size']; _, _, bw, bh = m['playable_bounds']
        lines.append(f"| {m['title']} | {w}×{h} | {bw}×{bh} | {bw*bh:,} | {len(m['starts'])} |")
    lines += ['', '**These originals do not resize when fewer players join.** Dune Patrol has only two',
        'original starts; a four-player variant would be a separately approved redesign, not an exact remake.',
        'Eight/six slots on the other maps do not establish fair tournament 1v1/2v2 slot assignments;',
        'those assignments and route lengths still need review.', '',
        '## Crucial grid difference', '',
        'The original snapshot uses `MapGrid.Type: Rectangular` (48×48 artwork). Current YMCA uses',
        '`RectangularIsometric` (128×64 artwork). Sprite pixel dimensions are NOT gameplay distances.',
        'Engine `Map.CenterOfCell` uses `(1024*x+512, 1024*y+512)` on the old grid and',
        '`(724*(x-y+1), 724*(x+y+1))` on the current grid. `MPos.ToCPos` staggers isometric rows.',
        'Consequently, equal width/height in rectangular map-memory coordinates is not an equally',
        'wide/tall world-space rectangle: in the new grid horizontal MPos steps span 1448 world units',
        'and vertical rows advance 724 (with alternating horizontal offsets).', '',
        'A naive copy of old MPos coordinates into new MPos coordinates stretches one world axis and',
        'compresses the other. For faithful remakes, preserve the old cell-neighbour graph using a',
        'translated CPos-to-CPos mapping (a near-isometric 45-degree rotation in world space), then',
        'derive the new enclosing map/bounds and protect unused exterior cells. Do not select new',
        'dimensions just by copying the old YAML numbers. The implemented optional CellBounds and',
        'BaseTerrainOverrides preserve this shape and its terrain semantics; see the validation report.', '',
        'The 80×80 rectangle has 69.4% as many cells as the old Glitters 96×96 rectangle, but 182.9%',
        'of Dune Patrol\'s 70×50 rectangle. This does not establish rush timing: start selection, topology,',
        'water/cliff occupancy and world-space geometry are still decisive.', '',
        '## Provenance and safeguards', '',
        f"- Pre-isometric Git source: `{originals['source_revision']}` (`6a0578fe^`).",
        '- Byte-exact files, original Dune Patrol package, starts, resource density totals and SHA-256 inventory:',
        '  `art-source/handcrafted/originals/manifest.json`.',
        '- `twosides` and `TwoSides` are identical at this revision; use the lowercase path to avoid',
        '  Windows case collisions. Exclude the distinct `TwoSidesAB` / Autobattle scenario.',
        '- The existing legacy-map archive contains a later partially converted Glitters version;',
        '  it is preserved but is NOT the original reference used here.',
        '- Original resource type IDs are recorded as raw IDs. They must be mapped through the old',
        '  tileset/rules, not silently reinterpreted as current Tiberium types.',
        '- Stored terrain elevations are zero in these originals; that does NOT make old cliff tiles',
        '  passable. Preserve the old terrain/actor movement restrictions independently of height.',
        '- No original map ID, package, replay, or Discord URL is replaced. New remakes need new IDs.',
        '- One deliberately close/rush-oriented generated archetype is desired; which one and the',
        '  revised sizes of the others are still decisions to make with the user.', '',
        '## Acceptance checks completed for the remakes', '',
        '1. Translate all original start cells, resources/densities and neutral actors without changing topology.',
        '2. Map original terrain passability and water/shore/buildable classes onto the new tileset.',
        '3. Verify original cell topology/terrain; test current-build native pathfinding, movement and yard placement. No old-build travel-time equivalence is claimed.',
        '4. Native visual review of terrain transitions, resource fields, shorelines and building placement.',
        '5. New packages are now locally enabled under Handcrafted Maps. Publishing and tournament-pool inclusion remain separate decisions.', '',
        '## Reproduce', '', '```sh',
        'python tools/prepare_handcrafted_sources.py',
        '# Build engine patches, engine/mod assemblies first; set ENGINE_DIR and MOD_SEARCH_PATHS.',
        'dotnet engine/bin/OpenRA.Utility.dll ca --map-size-report art-source/handcrafted/generator-sizes.json',
        'python tools/report_handcrafted_sizes.py',
        'python tools/test_handcrafted_sources.py -v', '```', '']
    target = ROOT / 'docs/handcrafted-maps.md'
    target.write_text('\n'.join(lines), encoding='utf-8')
    print(target)


if __name__ == '__main__':
    report()
