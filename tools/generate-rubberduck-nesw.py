from __future__ import annotations

from pathlib import Path

from PIL import Image


TILE_WIDTH = 128
TILE_HEIGHT = 64
EDGE_FRAME_INDICES = {
    "S": [32, 33],
    "W": [34, 35],
    "E": [36, 37],
    "N": [38, 39],
}

SOURCES = [
    "grass_a_over_dirt",
    "grass_a_over_sand",
    "grass_b_over_dirt",
    "grass_b_over_sand",
    "sand_over_dirt",
    "sand_over_grass_a",
    "sand_over_grass_b",
    "grass_a_over_dirt_dark",
    "grass_b_over_dirt_dark",
    "sand_over_dirt_dark",
]

BACKGROUND_BY_SOURCE = {
    "grass_a_over_dirt": "dirt_a_base",
    "grass_b_over_dirt": "dirt_a_base",
    "sand_over_dirt": "dirt_a_base",
    "grass_a_over_dirt_dark": "dirt_b_base",
    "grass_b_over_dirt_dark": "dirt_b_base",
    "sand_over_dirt_dark": "dirt_b_base",
    "grass_a_over_sand": "sand_base",
    "grass_b_over_sand": "sand_base",
    "sand_over_grass_a": "grass_a_base",
    "sand_over_grass_b": "grass_b_base",
}


def frame(sheet: Image.Image, index: int) -> Image.Image:
    cols = sheet.width // TILE_WIDTH
    x = (index % cols) * TILE_WIDTH
    y = (index // cols) * TILE_HEIGHT
    return sheet.crop((x, y, x + TILE_WIDTH, y + TILE_HEIGHT))


def build_nesw(sheet_path: Path, output_path: Path, background_path: Path | None) -> None:
    sheet = Image.open(sheet_path).convert("RGBA")

    if background_path is None:
        output = Image.new("RGBA", (TILE_WIDTH, TILE_HEIGHT), (0, 0, 0, 0))
    else:
        background_sheet = Image.open(background_path).convert("RGBA")
        output = frame(background_sheet, 0)

    composed = frame(sheet, EDGE_FRAME_INDICES["N"][0])
    composed = Image.alpha_composite(composed, frame(sheet, EDGE_FRAME_INDICES["E"][0]))
    composed = Image.alpha_composite(composed, frame(sheet, EDGE_FRAME_INDICES["S"][0]))
    composed = Image.alpha_composite(composed, frame(sheet, EDGE_FRAME_INDICES["W"][0]))
    output = Image.alpha_composite(output, composed)

    output.save(output_path)
    output_path.with_suffix(".yaml").write_text(
        "FrameSize: 128,64\nFrameAmount: 1\n",
        encoding="ascii",
    )


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    terrain_dir = root / "mods" / "ca" / "bits" / "terrain" / "rubberduck"

    for name in SOURCES:
        background_name = BACKGROUND_BY_SOURCE.get(name)
        for suffix in ("", "_shaded"):
            combo = terrain_dir / f"{name}_combo{suffix}.png"
            if combo.exists():
                src = combo
                background = None
            else:
                src = terrain_dir / f"{name}{suffix}.png"
                if not src.exists():
                    continue
                background = None
                if background_name is not None:
                    candidate = terrain_dir / f"{background_name}{suffix}.png"
                    if candidate.exists():
                        background = candidate

            dst = terrain_dir / f"{name}_nesw{suffix}.png"
            build_nesw(src, dst, background)


if __name__ == "__main__":
    main()
