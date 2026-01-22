from __future__ import annotations

from pathlib import Path


DEBUG_AUTOTILE = True


TERRAIN_SECTION = """Terrain:
\tTerrainType@Clear:
\t\tType: Clear
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: 284428
\t\tRestrictPlayerColor: true
\tTerrainType@Gems:
\t\tType: Gems
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: 8470FF
\t\tRestrictPlayerColor: true
\tTerrainType@Ore:
\t\tType: Ore
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: 948060
\t\tRestrictPlayerColor: true
\tTerrainType@Dirt:
\t\tType: Dirt
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: 5E430D
\tTerrainType@Sand:
\t\tType: Sand
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: B09C78
\tTerrainType@Cliff:
\t\tType: Cliff
\t\tTargetTypes: Ground
\t\tColor: 000000
\tTerrainType@Water:
\t\tType: Water
\t\tTargetTypes: Water
\t\tColor: 5C74A4
\t\tRestrictPlayerColor: true
\tTerrainType@BlueTiberium:
\t\tType: BlueTiberium
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: 54FCFC
\t\tRestrictPlayerColor: true
\tTerrainType@Tiberium:
\t\tType: Tiberium
\t\tTargetTypes: Ground
\t\tAcceptsSmudgeType: Crater, Scorch
\t\tColor: A1E21C
\t\tRestrictPlayerColor: true
"""


SHEET_FRAMES = {
	"NE": [16, 17, 18, 19],
	"SE": [20, 21, 22, 23],
	"NW": [24, 25, 26, 27],
	"SW": [28, 29, 30, 31],
	"S": [32, 33],
	"W": [34, 35],
	"E": [36, 37],
	"N": [38, 39],
	"WNE": [40, 41],
	"NES": [42, 43],
	"SWN": [44, 45],
	"ESW": [46, 47],
}

PATTERN_FRAMES = SHEET_FRAMES

PATTERN_ORDER = [
    "N",
    "E",
    "S",
    "W",
    "NE",
    "NW",
    "SE",
    "SW",
    "WNE",
    "NES",
    "SWN",
    "ESW",
]
NESW_OFFSET = len(PATTERN_ORDER)

AUTOTILE_PATTERN_OFFSETS = {
    "N": 4,    # NE frame
    "E": 6,    # SE frame
    "S": 7,    # SW frame
    "W": 5,    # NW frame
    "NE": 9,   # NES frame
    "NW": 8,   # WNE frame
    "SE": 11,  # ESW frame
    "SW": 10,  # SWN frame
    "WNE": 0,  # N frame
    "NES": 1,  # E frame
    "SWN": 2,  # S frame
    "ESW": 3,  # W frame
}


def format_frames(frames: list[int]) -> str:
    return ", ".join(str(f) for f in frames)


def emit_template(
    template_id: int,
    categories: str,
    image: str,
    terrain: str,
    frames: list[int] | None,
) -> str:
    lines: list[str] = []
    lines.append(f"\tTemplate@{template_id}:")
    lines.append(f"\t\tCategories: {categories}")
    lines.append(f"\t\tId: {template_id}")
    lines.append(f"\t\tImages: {image}")
    lines.append("\t\tSize: 1, 1")
    lines.append("\t\tPickAny: true")
    if frames is not None:
        lines.append(f"\t\tFrames: {format_frames(frames)}")
        tile_count = len(frames)
    else:
        tile_count = 16
    lines.append("\t\tTiles:")
    for i in range(tile_count):
        lines.append(f"\t\t\t{i}: {terrain}")
    return "\n".join(lines)


def generate_tileset_yaml(output: Path, shaded: bool) -> None:
    suffix = "_shaded" if shaded else ""
    tileset_id = "RUBBERDUCK-TEMPERATE-SHADED" if shaded else "RUBBERDUCK-TEMPERATE"
    tileset_name = "Rubberduck Temperate Shaded" if shaded else "Rubberduck Temperate"

    header = f"""General:
\tName: {tileset_name}
\tId: {tileset_id}
\tTileSize: 128, 64
\tSheetSize: 2048
\tEnableDepth: false
\tEditorTemplateOrder: Rubberduck Grass, Rubberduck Dirt, Rubberduck Sand, Rubberduck Transitions

{TERRAIN_SECTION}
Templates:
"""

    base_images = {
        "grass_a": f"bits/terrain/rubberduck/grass_a_base{suffix}.png",
        "grass_b": f"bits/terrain/rubberduck/grass_b_base{suffix}.png",
        "sand": f"bits/terrain/rubberduck/sand_base{suffix}.png",
        "dirt_a": f"bits/terrain/rubberduck/dirt_a_base{suffix}.png",
        "dirt_b": f"bits/terrain/rubberduck/dirt_b_base{suffix}.png",
    }

    transition_images = {
        "grass_a_over_dirt": f"bits/terrain/rubberduck/grass_a_over_dirt_combo{suffix}.png",
        "grass_a_over_sand": f"bits/terrain/rubberduck/grass_a_over_sand_combo{suffix}.png",
        "grass_b_over_dirt": f"bits/terrain/rubberduck/grass_b_over_dirt_combo{suffix}.png",
        "grass_b_over_sand": f"bits/terrain/rubberduck/grass_b_over_sand_combo{suffix}.png",
        "sand_over_dirt": f"bits/terrain/rubberduck/sand_over_dirt_combo{suffix}.png",
        "grass_a_over_dirt_dark": f"bits/terrain/rubberduck/grass_a_over_dirt_dark_combo{suffix}.png",
        "grass_b_over_dirt_dark": f"bits/terrain/rubberduck/grass_b_over_dirt_dark_combo{suffix}.png",
        "sand_over_dirt_dark": f"bits/terrain/rubberduck/sand_over_dirt_dark_combo{suffix}.png",
        "sand_over_grass_a": f"bits/terrain/rubberduck/sand_over_grass_a_combo{suffix}.png",
        "sand_over_grass_b": f"bits/terrain/rubberduck/sand_over_grass_b_combo{suffix}.png",
    }

    templates: list[str] = []

    templates.append(emit_template(1000, "Rubberduck Grass", base_images["grass_a"], "Clear", None))
    templates.append(emit_template(1010, "Rubberduck Grass", base_images["grass_b"], "Clear", None))
    templates.append(emit_template(1020, "Rubberduck Sand", base_images["sand"], "Sand", None))
    templates.append(emit_template(1030, "Rubberduck Dirt", base_images["dirt_a"], "Dirt", None))
    templates.append(emit_template(1040, "Rubberduck Dirt", base_images["dirt_b"], "Dirt", None))

    def add_transition_set(base_id: int, image: str, nesw_image: str, terrain: str = "Clear") -> None:
        for i, pattern in enumerate(PATTERN_ORDER):
            templates.append(
                emit_template(
                    base_id + i,
                    "Rubberduck Transitions",
                    image,
                    terrain,
                    PATTERN_FRAMES[pattern],
                )
            )
        templates.append(
            emit_template(
                base_id + NESW_OFFSET,
                "Rubberduck Transitions",
                nesw_image,
                terrain,
                [0],
            )
        )

    nesw_images = {
        "grass_a_over_dirt": f"bits/terrain/rubberduck/grass_a_over_dirt_nesw{suffix}.png",
        "grass_a_over_sand": f"bits/terrain/rubberduck/grass_a_over_sand_nesw{suffix}.png",
        "grass_b_over_dirt": f"bits/terrain/rubberduck/grass_b_over_dirt_nesw{suffix}.png",
        "grass_b_over_sand": f"bits/terrain/rubberduck/grass_b_over_sand_nesw{suffix}.png",
        "sand_over_dirt": f"bits/terrain/rubberduck/sand_over_dirt_nesw{suffix}.png",
        "grass_a_over_dirt_dark": f"bits/terrain/rubberduck/grass_a_over_dirt_dark_nesw{suffix}.png",
        "grass_b_over_dirt_dark": f"bits/terrain/rubberduck/grass_b_over_dirt_dark_nesw{suffix}.png",
        "sand_over_dirt_dark": f"bits/terrain/rubberduck/sand_over_dirt_dark_nesw{suffix}.png",
        "sand_over_grass_a": f"bits/terrain/rubberduck/sand_over_grass_a_nesw{suffix}.png",
        "sand_over_grass_b": f"bits/terrain/rubberduck/sand_over_grass_b_nesw{suffix}.png",
    }

    add_transition_set(1100, transition_images["grass_a_over_dirt"], nesw_images["grass_a_over_dirt"])
    add_transition_set(1120, transition_images["grass_a_over_sand"], nesw_images["grass_a_over_sand"])
    add_transition_set(1140, transition_images["grass_b_over_dirt"], nesw_images["grass_b_over_dirt"])
    add_transition_set(1160, transition_images["grass_b_over_sand"], nesw_images["grass_b_over_sand"])
    add_transition_set(1180, transition_images["sand_over_dirt"], nesw_images["sand_over_dirt"])
    add_transition_set(1200, transition_images["grass_a_over_dirt_dark"], nesw_images["grass_a_over_dirt_dark"])
    add_transition_set(1220, transition_images["grass_b_over_dirt_dark"], nesw_images["grass_b_over_dirt_dark"])
    add_transition_set(1240, transition_images["sand_over_dirt_dark"], nesw_images["sand_over_dirt_dark"])
    add_transition_set(1260, transition_images["sand_over_grass_a"], nesw_images["sand_over_grass_a"], terrain="Sand")
    add_transition_set(1280, transition_images["sand_over_grass_b"], nesw_images["sand_over_grass_b"], terrain="Sand")

    output.write_text(header + "\n\n".join(templates) + "\n", encoding="ascii")


def generate_autotile_yaml(output: Path) -> None:
    def transition_block(base_id: int, indent: str = "\t\t\t\t\t\t") -> str:
        lines = []
        for pattern in PATTERN_ORDER:
            offset = AUTOTILE_PATTERN_OFFSETS[pattern]
            lines.append(f"{indent}{pattern}: {base_id + offset}")
        lines.append(f"{indent}NESW: {base_id + NESW_OFFSET}")
        return "\n".join(lines)

    world_block = f"""\tAutoTile:
\t\tDebug: {str(DEBUG_AUTOTILE).lower()}
\t\tUseHigherPriority: false
\t\tInvertMask: false
\t\tIncludeDiagonals: false
\t\tAllowEnclosedTransitions: true
\t\tIgnoreNonBaseTemplates: true
\t\tGroups:
\t\t\tDirt:
\t\t\t\tPriority: 1
\t\t\tSand:
\t\t\t\tPriority: 2
\t\t\tGrass:
\t\t\t\tPriority: 3
\t\tStyles:
\t\t\tGrassStone:
\t\t\t\tGroup: Grass
\t\t\t\tBaseTemplate: 1000
\t\t\t\tTransitions:
\t\t\t\t\tDirtLight:
{transition_block(1100)}
\t\t\t\t\tSand:
{transition_block(1120)}
\t\t\t\t\tDirtDark:
{transition_block(1200)}
\t\t\tGrassPlain:
\t\t\t\tGroup: Grass
\t\t\t\tBaseTemplate: 1010
\t\t\t\tTransitions:
\t\t\t\t\tDirtLight:
{transition_block(1140)}
\t\t\t\t\tSand:
{transition_block(1160)}
\t\t\t\t\tDirtDark:
{transition_block(1220)}
\t\t\tSand:
\t\t\t\tGroup: Sand
\t\t\t\tBaseTemplate: 1020
\t\t\t\tTransitions:
\t\t\t\t\tDirtLight:
{transition_block(1180)}
\t\t\t\t\tDirtDark:
{transition_block(1240)}
\t\t\tDirtLight:
\t\t\t\tGroup: Dirt
\t\t\t\tBaseTemplate: 1030
\t\t\tDirtDark:
\t\t\t\tGroup: Dirt
\t\t\t\tBaseTemplate: 1040
"""

    autotile = f"""World:
{world_block}

EditorWorld:
{world_block}
"""

    output.write_text(autotile, encoding="ascii")


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    tilesets_dir = root / "mods" / "ca" / "tilesets"
    rules_dir = root / "mods" / "ca" / "rules"
    tilesets_dir.mkdir(parents=True, exist_ok=True)
    rules_dir.mkdir(parents=True, exist_ok=True)

    generate_tileset_yaml(tilesets_dir / "rubberduck-temperate.yaml", shaded=False)
    generate_tileset_yaml(tilesets_dir / "rubberduck-temperate-shaded.yaml", shaded=True)
    generate_autotile_yaml(rules_dir / "autotile.yaml")


if __name__ == "__main__":
    main()
