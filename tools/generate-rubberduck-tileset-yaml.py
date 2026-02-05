from __future__ import annotations

from math import ceil
from pathlib import Path


DEBUG_AUTOTILE = True
FRAME_SIZE = (128, 64)
BASE_FRAME_AMOUNT = 80
SOURCE_BLOCK_COLS = 8
SOURCE_BLOCK_ROWS = 10


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

SPECIAL_PATTERN_ORDER = [
    "Hole",
    "U_NW",
    "U_NE",
    "U_SE",
    "U_SW",
    "Parallel_NE_SW",
    "Parallel_NW_SE",
]

END_PATTERN_ORDER = [
    "End_NW",
    "End_NE",
    "End_SE",
    "End_SW",
]
SPECIAL_PATTERN_BASE_OFFSET = NESW_OFFSET + 1
SPECIAL_PATTERN_OFFSETS = {
    pattern: SPECIAL_PATTERN_BASE_OFFSET + i
    for i, pattern in enumerate(SPECIAL_PATTERN_ORDER)
}
END_PATTERN_BASE = 1300
END_PATTERN_OFFSETS = {pattern: i for i, pattern in enumerate(END_PATTERN_ORDER)}

AUTOTILE_PATTERN_OFFSETS = {
    "N": 4,   # NE frame
    "E": 6,   # SE frame
    "S": 7,   # SW frame
    "W": 5,   # NW frame
    "NE": 1,  # E frame
    "NW": 0,  # N frame
    "SE": 2,  # S frame
    "SW": 3,  # W frame
    "WNE": 8,
    "NES": 9,
    "SWN": 10,
    "ESW": 11,
}

SPECIAL_PATTERN_FRAMES = {
    "Hole": [80, 81],
    "U_NW": [82, 83],
    "U_NE": [84, 85],
    "U_SE": [86, 87],
    "U_SW": [88, 89],
    "Parallel_NE_SW": [90, 91, 92, 93],
    "Parallel_NW_SE": [94, 95, 96, 97],
    "End_NW": [98, 99],
    "End_NE": [100, 101],
    "End_SE": [102, 103],
    "End_SW": [104, 105],
}

PATTERN_ALPHA_MODE = {
    "Hole": "min",
    "U_NW": "min",
    "U_NE": "min",
    "U_SE": "min",
    "U_SW": "min",
    "Parallel_NE_SW": "min",
    "Parallel_NW_SE": "min",
    "End_NW": "min",
    "End_NE": "min",
    "End_SE": "min",
    "End_SW": "min",
}

COMBO_DIFF_THRESHOLD = 4
COMBO_ALPHA_SCALE = 3

SPECIAL_PATTERNS_FROM_COMBO = set()
ALL_SPECIAL_PATTERNS = SPECIAL_PATTERN_ORDER + END_PATTERN_ORDER

SPECIAL_PATTERN_SOURCES = {
    "Hole": [
        [SHEET_FRAMES["WNE"][0], SHEET_FRAMES["NES"][0], SHEET_FRAMES["SWN"][0], SHEET_FRAMES["ESW"][0]],
        [SHEET_FRAMES["WNE"][1], SHEET_FRAMES["NES"][1], SHEET_FRAMES["SWN"][1], SHEET_FRAMES["ESW"][1]],
    ],
    "U_NW": [
        [SHEET_FRAMES["WNE"][0], SHEET_FRAMES["SWN"][0]],
        [SHEET_FRAMES["WNE"][1], SHEET_FRAMES["SWN"][1]],
    ],
    "U_NE": [
        [SHEET_FRAMES["WNE"][0], SHEET_FRAMES["NES"][0]],
        [SHEET_FRAMES["WNE"][1], SHEET_FRAMES["NES"][1]],
    ],
    "U_SE": [
        [SHEET_FRAMES["NES"][0], SHEET_FRAMES["ESW"][0]],
        [SHEET_FRAMES["NES"][1], SHEET_FRAMES["ESW"][1]],
    ],
    "U_SW": [
        [SHEET_FRAMES["ESW"][0], SHEET_FRAMES["SWN"][0]],
        [SHEET_FRAMES["ESW"][1], SHEET_FRAMES["SWN"][1]],
    ],
    "Parallel_NE_SW": [
        [SHEET_FRAMES["NE"][0], SHEET_FRAMES["SW"][0]],
        [SHEET_FRAMES["NE"][1], SHEET_FRAMES["SW"][1]],
        [SHEET_FRAMES["NE"][2], SHEET_FRAMES["SW"][2]],
        [SHEET_FRAMES["NE"][3], SHEET_FRAMES["SW"][3]],
    ],
    "Parallel_NW_SE": [
        [SHEET_FRAMES["NW"][0], SHEET_FRAMES["SE"][0]],
        [SHEET_FRAMES["NW"][1], SHEET_FRAMES["SE"][1]],
        [SHEET_FRAMES["NW"][2], SHEET_FRAMES["SE"][2]],
        [SHEET_FRAMES["NW"][3], SHEET_FRAMES["SE"][3]],
    ],
    "End_NW": [
        [SHEET_FRAMES["N"][0], SHEET_FRAMES["W"][0]],
        [SHEET_FRAMES["N"][1], SHEET_FRAMES["W"][1]],
    ],
    "End_NE": [
        [SHEET_FRAMES["N"][0], SHEET_FRAMES["E"][0]],
        [SHEET_FRAMES["N"][1], SHEET_FRAMES["E"][1]],
    ],
    "End_SE": [
        [SHEET_FRAMES["S"][0], SHEET_FRAMES["E"][0]],
        [SHEET_FRAMES["S"][1], SHEET_FRAMES["E"][1]],
    ],
    "End_SW": [
        [SHEET_FRAMES["S"][0], SHEET_FRAMES["W"][0]],
        [SHEET_FRAMES["S"][1], SHEET_FRAMES["W"][1]],
    ],
}

TRANSITION_IMAGE_KEYS = [
    "grass_a_over_dirt",
    "grass_a_over_sand",
    "grass_b_over_dirt",
    "grass_b_over_sand",
    "sand_over_dirt",
    "grass_a_over_dirt_dark",
    "grass_b_over_dirt_dark",
    "sand_over_dirt_dark",
    "sand_over_grass_a",
    "sand_over_grass_b",
]

BASE_IMAGE_BY_TRANSITION = {
    "grass_a_over_dirt": "dirt_a",
    "grass_a_over_sand": "sand",
    "grass_b_over_dirt": "dirt_a",
    "grass_b_over_sand": "sand",
    "sand_over_dirt": "dirt_a",
    "grass_a_over_dirt_dark": "dirt_b",
    "grass_b_over_dirt_dark": "dirt_b",
    "sand_over_dirt_dark": "dirt_b",
    "sand_over_grass_a": "grass_a",
    "sand_over_grass_b": "grass_b",
}

TRANSITION_SOURCES = {
    "grass_a_over_dirt": {"overlay": "grass_a", "base": "dirt_a"},
    "grass_a_over_sand": {"overlay": "grass_a", "base": "sand"},
    "grass_b_over_dirt": {"overlay": "grass_b", "base": "dirt_a"},
    "grass_b_over_sand": {"overlay": "grass_b", "base": "sand"},
    "sand_over_dirt": {"overlay": "sand", "base": "dirt_a"},
    "grass_a_over_dirt_dark": {"overlay": "grass_a", "base": "dirt_b"},
    "grass_b_over_dirt_dark": {"overlay": "grass_b", "base": "dirt_b"},
    "sand_over_dirt_dark": {"overlay": "sand", "base": "dirt_b"},
    "sand_over_grass_a": {
        "overlay": "grass_a",
        "base": "grass_a",
        "fill": "sand",
        "inverted": True,
        "special_overlay": "sand",
        "special_inverted": False,
    },
    "sand_over_grass_b": {
        "overlay": "grass_b",
        "base": "grass_b",
        "fill": "sand",
        "inverted": True,
        "special_overlay": "sand",
        "special_inverted": False,
    },
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

    def add_transition_set(transition_key: str, base_id: int, image: str, nesw_image: str, terrain: str = "Clear") -> None:
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
        for pattern in SPECIAL_PATTERN_ORDER:
            templates.append(
                emit_template(
                    base_id + SPECIAL_PATTERN_OFFSETS[pattern],
                    "Rubberduck Transitions",
                    image,
                    terrain,
                    SPECIAL_PATTERN_FRAMES[pattern],
                )
            )

        transition_index = TRANSITION_IMAGE_KEYS.index(transition_key)
        end_base_id = END_PATTERN_BASE + transition_index * len(END_PATTERN_ORDER)
        for pattern in END_PATTERN_ORDER:
            templates.append(
                emit_template(
                    end_base_id + END_PATTERN_OFFSETS[pattern],
                    "Rubberduck Transitions",
                    image,
                    terrain,
                    SPECIAL_PATTERN_FRAMES[pattern],
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

    add_transition_set("grass_a_over_dirt", 1100, transition_images["grass_a_over_dirt"], nesw_images["grass_a_over_dirt"])
    add_transition_set("grass_a_over_sand", 1120, transition_images["grass_a_over_sand"], nesw_images["grass_a_over_sand"])
    add_transition_set("grass_b_over_dirt", 1140, transition_images["grass_b_over_dirt"], nesw_images["grass_b_over_dirt"])
    add_transition_set("grass_b_over_sand", 1160, transition_images["grass_b_over_sand"], nesw_images["grass_b_over_sand"])
    add_transition_set("sand_over_dirt", 1180, transition_images["sand_over_dirt"], nesw_images["sand_over_dirt"])
    add_transition_set("grass_a_over_dirt_dark", 1200, transition_images["grass_a_over_dirt_dark"], nesw_images["grass_a_over_dirt_dark"])
    add_transition_set("grass_b_over_dirt_dark", 1220, transition_images["grass_b_over_dirt_dark"], nesw_images["grass_b_over_dirt_dark"])
    add_transition_set("sand_over_dirt_dark", 1240, transition_images["sand_over_dirt_dark"], nesw_images["sand_over_dirt_dark"])
    add_transition_set("sand_over_grass_a", 1260, transition_images["sand_over_grass_a"], nesw_images["sand_over_grass_a"], terrain="Sand")
    add_transition_set("sand_over_grass_b", 1280, transition_images["sand_over_grass_b"], nesw_images["sand_over_grass_b"], terrain="Sand")

    output.write_text(header + "\n\n".join(templates) + "\n", encoding="ascii")


def generate_autotile_yaml(output: Path) -> None:
    def transition_block(transition_key: str, base_id: int, indent: str = "\t\t\t\t\t\t") -> str:
        lines = []
        for pattern in PATTERN_ORDER:
            offset = AUTOTILE_PATTERN_OFFSETS[pattern]
            lines.append(f"{indent}{pattern}: {base_id + offset}")
        lines.append(f"{indent}NESW: {base_id + NESW_OFFSET}")
        for pattern in SPECIAL_PATTERN_ORDER:
            offset = SPECIAL_PATTERN_OFFSETS[pattern]
            lines.append(f"{indent}{pattern}: {base_id + offset}")
        transition_index = TRANSITION_IMAGE_KEYS.index(transition_key)
        end_base_id = END_PATTERN_BASE + transition_index * len(END_PATTERN_ORDER)
        for pattern in END_PATTERN_ORDER:
            lines.append(f"{indent}{pattern}: {end_base_id + END_PATTERN_OFFSETS[pattern]}")
        return "\n".join(lines)

    world_block = f"""\tAutoTile:
\t\tDebug: {str(DEBUG_AUTOTILE).lower()}
\t\tUseHigherPriority: false
\t\tInvertMask: false
\t\tIncludeDiagonals: true
\t\tAllowEnclosedTransitions: true
\t\tIgnoreNonBaseTemplates: false
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
{transition_block("grass_a_over_dirt", 1100)}
\t\t\t\t\tSand:
{transition_block("grass_a_over_sand", 1120)}
\t\t\t\t\tDirtDark:
{transition_block("grass_a_over_dirt_dark", 1200)}
\t\t\tGrassPlain:
\t\t\t\tGroup: Grass
\t\t\t\tBaseTemplate: 1010
\t\t\t\tTransitions:
\t\t\t\t\tDirtLight:
{transition_block("grass_b_over_dirt", 1140)}
\t\t\t\t\tSand:
{transition_block("grass_b_over_sand", 1160)}
\t\t\t\t\tDirtDark:
{transition_block("grass_b_over_dirt_dark", 1220)}
\t\t\tSand:
\t\t\t\tGroup: Sand
\t\t\t\tBaseTemplate: 1020
\t\t\t\tTransitions:
\t\t\t\t\tDirtLight:
{transition_block("sand_over_dirt", 1180)}
\t\t\t\t\tDirtDark:
{transition_block("sand_over_dirt_dark", 1240)}
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


def generate_special_transition_images(root: Path) -> None:
    try:
        from PIL import Image, ImageChops
    except Exception as exc:
        raise RuntimeError("PIL is required to generate special transition images.") from exc

    rubberduck_dir = root / "mods" / "ca" / "bits" / "terrain" / "rubberduck"
    source_dir = root.parent / "rubberduck terrain"
    frame_w, frame_h = FRAME_SIZE
    extra_frames = sum(len(SPECIAL_PATTERN_FRAMES[p]) for p in ALL_SPECIAL_PATTERNS)
    new_frame_amount = BASE_FRAME_AMOUNT + extra_frames

    def frame_rect(index: int, frames_per_row: int) -> tuple[int, int, int, int]:
        row = index // frames_per_row
        col = index % frames_per_row
        x0 = col * frame_w
        y0 = row * frame_h
        return (x0, y0, x0 + frame_w, y0 + frame_h)

    def overlay_frame_rect(index: int, block_index: int) -> tuple[int, int, int, int]:
        row = index // SOURCE_BLOCK_COLS
        col = index % SOURCE_BLOCK_COLS
        x0 = col * frame_w
        y0 = (block_index * SOURCE_BLOCK_ROWS + row) * frame_h
        return (x0, y0, x0 + frame_w, y0 + frame_h)

    def source_block_index(terrain_key: str, shaded: bool) -> int:
        if terrain_key == "grass_a":
            return 1 if shaded else 0
        if terrain_key == "grass_b":
            return 3 if shaded else 2
        if terrain_key == "dirt_a":
            return 1 if shaded else 0
        if terrain_key == "dirt_b":
            return 3 if shaded else 2
        if terrain_key == "sand":
            return 1 if shaded else 0
        raise ValueError(f"Unknown terrain key: {terrain_key}")

    def source_sheet_name(terrain_key: str) -> str:
        if terrain_key.startswith("grass"):
            return "grass"
        if terrain_key.startswith("sand"):
            return "sand"
        if terrain_key.startswith("dirt"):
            return "dirt"
        raise ValueError(f"Unknown terrain key: {terrain_key}")

    def build_overlay(
        base_image,
        overlay_source,
        base_index: int,
        overlay_indices: list[int],
        overlay_block: int,
        alpha_mode: str,
    ):
        color = Image.new("RGBA", (frame_w, frame_h), (0, 0, 0, 0))
        alpha_min = None
        alpha_max = Image.new("L", (frame_w, frame_h), 0)
        for idx in overlay_indices:
            piece = overlay_source.crop(overlay_frame_rect(idx, overlay_block)).convert("RGBA")
            color = Image.alpha_composite(color, piece)
            alpha = piece.split()[3]
            alpha_min = alpha if alpha_min is None else ImageChops.darker(alpha_min, alpha)
            alpha_max = ImageChops.lighter(alpha_max, alpha)

        r, g, b, _ = color.split()
        if alpha_mode == "min" and alpha_min is not None:
            overlay_alpha = alpha_min
        elif alpha_mode == "max":
            overlay_alpha = alpha_max
        else:
            overlay_alpha = color.split()[3]
        return Image.merge("RGBA", (r, g, b, overlay_alpha))

    def build_overlay_from_combo(
        base_frame,
        combo_frame,
        diff_threshold: int,
        alpha_scale: int,
    ):
        overlay = Image.new("RGBA", base_frame.size, (0, 0, 0, 0))
        overlay_px = overlay.load()
        base_px = base_frame.load()
        combo_px = combo_frame.load()
        width, height = base_frame.size

        for y in range(height):
            for x in range(width):
                br, bg, bb, _ = base_px[x, y]
                cr, cg, cb, _ = combo_px[x, y]
                diff = max(abs(cr - br), abs(cg - bg), abs(cb - bb))
                if diff < diff_threshold:
                    continue
                alpha = min(255, (diff - diff_threshold) * alpha_scale)
                overlay_px[x, y] = (cr, cg, cb, alpha)

        return overlay

    def composite_frames(
        base_image,
        overlay_source,
        base_index: int,
        overlay_indices: list[int],
        overlay_block: int,
        alpha_mode: str,
    ):
        base_frames_per_row = base_image.width // frame_w
        base_frame = base_image.crop(frame_rect(base_index, base_frames_per_row)).convert("RGBA")
        overlay = build_overlay(
            base_image,
            overlay_source,
            base_index,
            overlay_indices,
            overlay_block,
            alpha_mode,
        )
        return Image.alpha_composite(base_frame, overlay)

    def composite_inverted_frames(
        base_image,
        fill_image,
        overlay_source,
        base_index: int,
        overlay_indices: list[int],
        overlay_block: int,
        alpha_mode: str,
    ):
        base_frames_per_row = base_image.width // frame_w
        fill_frames_per_row = fill_image.width // frame_w
        base_frame = base_image.crop(frame_rect(base_index, base_frames_per_row)).convert("RGBA")
        fill_frame = fill_image.crop(frame_rect(base_index, fill_frames_per_row)).convert("RGBA")

        alpha_min = None
        alpha_max = Image.new("L", (frame_w, frame_h), 0)
        for idx in overlay_indices:
            piece = overlay_source.crop(overlay_frame_rect(idx, overlay_block)).convert("RGBA")
            alpha = piece.split()[3]
            alpha_min = alpha if alpha_min is None else ImageChops.darker(alpha_min, alpha)
            alpha_max = ImageChops.lighter(alpha_max, alpha)

        if alpha_min is None:
            return base_frame

        if alpha_mode == "min":
            alpha = alpha_min
        elif alpha_mode == "max":
            alpha = alpha_max
        else:
            alpha = alpha_min

        inv_alpha = Image.eval(alpha, lambda a: 255 - a)
        fill_frame.putalpha(inv_alpha)
        return Image.alpha_composite(base_frame, fill_frame)

    def update_frame_amount(meta_path: Path) -> None:
        if not meta_path.exists():
            return
        lines = meta_path.read_text(encoding="ascii").splitlines()
        updated = []
        replaced = False
        for line in lines:
            if line.startswith("FrameAmount:"):
                updated.append(f"FrameAmount: {new_frame_amount}")
                replaced = True
            else:
                updated.append(line)
        if not replaced:
            updated.append(f"FrameAmount: {new_frame_amount}")
        meta_path.write_text("\n".join(updated) + "\n", encoding="ascii")

    if not source_dir.exists():
        raise FileNotFoundError(f"Source tileset directory not found: {source_dir}")

    for key in TRANSITION_IMAGE_KEYS:
        for suffix in ("", "_shaded"):
            shaded = suffix == "_shaded"
            source_spec = TRANSITION_SOURCES[key]
            overlay_key = source_spec["overlay"]
            base_key = source_spec["base"]
            inverted = source_spec.get("inverted", False)
            special_overlay_key = source_spec.get("special_overlay", overlay_key)
            special_inverted = source_spec.get("special_inverted", inverted)
            image_path = rubberduck_dir / f"{key}_combo{suffix}.png"
            meta_path = rubberduck_dir / f"{key}_combo{suffix}.yaml"
            base_path = rubberduck_dir / f"{base_key}_base{suffix}.png"
            overlay_sheet = source_sheet_name(overlay_key)
            overlay_block = source_block_index(overlay_key, shaded)
            overlay_path = source_dir / f"{overlay_sheet}_tiles_w_trans.png"
            special_overlay_sheet = source_sheet_name(special_overlay_key)
            special_overlay_block = source_block_index(special_overlay_key, shaded)
            special_overlay_path = source_dir / f"{special_overlay_sheet}_tiles_w_trans.png"
            fill_image = None
            if inverted:
                fill_key = source_spec["fill"]
                fill_path = rubberduck_dir / f"{fill_key}_base{suffix}.png"
                if not fill_path.exists():
                    raise FileNotFoundError(f"Fill image not found: {fill_path}")
                fill_image = Image.open(fill_path).convert("RGBA")
            if not image_path.exists():
                continue

            if not base_path.exists():
                raise FileNotFoundError(f"Base image not found: {base_path}")

            img = Image.open(image_path).convert("RGBA")
            if not overlay_path.exists():
                raise FileNotFoundError(f"Overlay source not found: {overlay_path}")
            overlay_source = Image.open(overlay_path).convert("RGBA")
            if not special_overlay_path.exists():
                raise FileNotFoundError(f"Special overlay source not found: {special_overlay_path}")
            special_overlay_source = Image.open(special_overlay_path).convert("RGBA")
            base_image = Image.open(base_path).convert("RGBA")

            total_rows = ceil(new_frame_amount / (img.width // frame_w))
            new_height = total_rows * frame_h
            new_image = Image.new("RGBA", (img.width, new_height), (0, 0, 0, 0))
            new_image.paste(img, (0, 0))

            for pattern in ALL_SPECIAL_PATTERNS:
                targets = SPECIAL_PATTERN_FRAMES[pattern]
                sources = SPECIAL_PATTERN_SOURCES[pattern]
                if len(targets) != len(sources):
                    raise ValueError(f"Pattern {pattern} frame mismatch: {len(targets)} targets vs {len(sources)} sources")
                for target_idx, source_indices in zip(targets, sources):
                    base_idx = 0
                    alpha_mode = PATTERN_ALPHA_MODE.get(pattern, "max")
                    if pattern in SPECIAL_PATTERNS_FROM_COMBO:
                        base_frames_per_row = base_image.width // frame_w
                        base_frame = base_image.crop(frame_rect(base_idx, base_frames_per_row)).convert("RGBA")
                        combined_overlay = Image.new("RGBA", (frame_w, frame_h), (0, 0, 0, 0))
                        for source_idx in source_indices:
                            combo_frame = img.crop(frame_rect(source_idx, img.width // frame_w)).convert("RGBA")
                            overlay = build_overlay_from_combo(
                                base_frame,
                                combo_frame,
                                COMBO_DIFF_THRESHOLD,
                                COMBO_ALPHA_SCALE,
                            )
                            combined_overlay = Image.alpha_composite(combined_overlay, overlay)
                        composed = Image.alpha_composite(base_frame, combined_overlay)
                    elif special_inverted and fill_image is not None:
                        composed = composite_inverted_frames(
                            base_image,
                            fill_image,
                            special_overlay_source,
                            base_idx,
                            source_indices,
                            special_overlay_block,
                            alpha_mode,
                        )
                    else:
                        composed = composite_frames(
                            base_image,
                            special_overlay_source,
                            base_idx,
                            source_indices,
                            special_overlay_block,
                            alpha_mode,
                        )
                    x0, y0, x1, y1 = frame_rect(target_idx, img.width // frame_w)
                    new_image.paste(composed, (x0, y0))

            new_image.save(image_path)
            update_frame_amount(meta_path)


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    tilesets_dir = root / "mods" / "ca" / "tilesets"
    rules_dir = root / "mods" / "ca" / "rules"
    tilesets_dir.mkdir(parents=True, exist_ok=True)
    rules_dir.mkdir(parents=True, exist_ok=True)

    generate_special_transition_images(root)
    generate_tileset_yaml(tilesets_dir / "rubberduck-temperate.yaml", shaded=False)
    generate_tileset_yaml(tilesets_dir / "rubberduck-temperate-shaded.yaml", shaded=True)
    generate_autotile_yaml(rules_dir / "autotile.yaml")


if __name__ == "__main__":
    main()
