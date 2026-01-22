from __future__ import annotations

from pathlib import Path

from PIL import Image


TILE_WIDTH = 128
TILE_HEIGHT = 64

SOURCES = {
    "grass_a_over_dirt": "dirt_a_base",
    "grass_a_over_sand": "sand_base",
    "grass_b_over_dirt": "dirt_a_base",
    "grass_b_over_sand": "sand_base",
    "sand_over_dirt": "dirt_a_base",
    "grass_a_over_dirt_dark": "dirt_b_base",
    "grass_b_over_dirt_dark": "dirt_b_base",
    "sand_over_dirt_dark": "dirt_b_base",
}

INVERTED_SOURCES = {
    "sand_over_grass_a": {
        "overlay": "grass_a_over_sand",
        "base": "grass_a_base",
        "fill": "sand_base",
    },
    "sand_over_grass_b": {
        "overlay": "grass_b_over_sand",
        "base": "grass_b_base",
        "fill": "sand_base",
    },
}


def composite_sheet(overlay_path: Path, base_path: Path, output_path: Path) -> None:
    overlay = Image.open(overlay_path).convert("RGBA")
    base = Image.open(base_path).convert("RGBA")

    cols = overlay.width // TILE_WIDTH
    rows = overlay.height // TILE_HEIGHT
    base_cols = base.width // TILE_WIDTH
    base_rows = base.height // TILE_HEIGHT

    output = Image.new("RGBA", overlay.size, (0, 0, 0, 0))

    for row in range(rows):
        for col in range(cols):
            x = col * TILE_WIDTH
            y = row * TILE_HEIGHT
            overlay_frame = overlay.crop((x, y, x + TILE_WIDTH, y + TILE_HEIGHT))

            base_row = row % base_rows
            base_col = col % base_cols
            bx = base_col * TILE_WIDTH
            by = base_row * TILE_HEIGHT
            base_frame = base.crop((bx, by, bx + TILE_WIDTH, by + TILE_HEIGHT))

            composite = Image.alpha_composite(base_frame, overlay_frame)
            output.paste(composite, (x, y))

    output.save(output_path)
    output_path.with_suffix(".yaml").write_text(
        f"FrameSize: {TILE_WIDTH},{TILE_HEIGHT}\nFrameAmount: {cols * rows}\n",
        encoding="ascii",
    )


def composite_inverted_sheet(overlay_path: Path, base_path: Path, fill_path: Path, output_path: Path) -> None:
    overlay = Image.open(overlay_path).convert("RGBA")
    base = Image.open(base_path).convert("RGBA")
    fill = Image.open(fill_path).convert("RGBA")

    cols = overlay.width // TILE_WIDTH
    rows = overlay.height // TILE_HEIGHT
    base_cols = base.width // TILE_WIDTH
    base_rows = base.height // TILE_HEIGHT
    fill_cols = fill.width // TILE_WIDTH
    fill_rows = fill.height // TILE_HEIGHT

    output = Image.new("RGBA", overlay.size, (0, 0, 0, 0))

    for row in range(rows):
        for col in range(cols):
            x = col * TILE_WIDTH
            y = row * TILE_HEIGHT
            overlay_frame = overlay.crop((x, y, x + TILE_WIDTH, y + TILE_HEIGHT))

            base_row = row % base_rows
            base_col = col % base_cols
            bx = base_col * TILE_WIDTH
            by = base_row * TILE_HEIGHT
            base_frame = base.crop((bx, by, bx + TILE_WIDTH, by + TILE_HEIGHT))

            fill_row = row % fill_rows
            fill_col = col % fill_cols
            fx = fill_col * TILE_WIDTH
            fy = fill_row * TILE_HEIGHT
            fill_frame = fill.crop((fx, fy, fx + TILE_WIDTH, fy + TILE_HEIGHT))

            alpha = overlay_frame.split()[3]
            inv_alpha = Image.eval(alpha, lambda a: 255 - a)
            fill_frame.putalpha(inv_alpha)

            composite = Image.alpha_composite(base_frame, fill_frame)
            output.paste(composite, (x, y))

    output.save(output_path)
    output_path.with_suffix(".yaml").write_text(
        f"FrameSize: {TILE_WIDTH},{TILE_HEIGHT}\nFrameAmount: {cols * rows}\n",
        encoding="ascii",
    )


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    terrain_dir = root / "mods" / "ca" / "bits" / "terrain" / "rubberduck"

    for overlay_name, base_name in SOURCES.items():
        for suffix in ("", "_shaded"):
            overlay_path = terrain_dir / f"{overlay_name}{suffix}.png"
            base_path = terrain_dir / f"{base_name}{suffix}.png"
            if not overlay_path.exists() or not base_path.exists():
                continue

            output_path = terrain_dir / f"{overlay_name}_combo{suffix}.png"
            composite_sheet(overlay_path, base_path, output_path)

    for output_name, sources in INVERTED_SOURCES.items():
        for suffix in ("", "_shaded"):
            overlay_path = terrain_dir / f"{sources['overlay']}{suffix}.png"
            base_path = terrain_dir / f"{sources['base']}{suffix}.png"
            fill_path = terrain_dir / f"{sources['fill']}{suffix}.png"
            if not overlay_path.exists() or not base_path.exists() or not fill_path.exists():
                continue

            output_path = terrain_dir / f"{output_name}_combo{suffix}.png"
            composite_inverted_sheet(overlay_path, base_path, fill_path, output_path)


if __name__ == "__main__":
    main()
