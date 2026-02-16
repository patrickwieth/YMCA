#!/usr/bin/env python3
"""
Reapply AutoTile transitions to a .oramap using the rules in mods/ca/rules/autotile.yaml.

Usage:
  python tools/autotile-map.py path/to/map.oramap
  python tools/autotile-map.py path/to/map.oramap --dry-run
  python tools/autotile-map.py path/to/map.oramap --marker-id 1060 --group-id Sand
  python tools/autotile-map.py path/to/map.oramap --marker-id 1060 --window 14
  python tools/autotile-map.py path/to/map.oramap --verify-marker 1050 --verify-marker 1060
"""
from __future__ import annotations

import argparse
import re
import struct
import zipfile
from dataclasses import dataclass
from pathlib import Path

import yaml


RECT_NEIGHBOR_STEPS = [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)]
RECT_DIAGONAL_STEPS = [(1, -1, 1 | 2), (1, 1, 2 | 4), (-1, 1, 4 | 8), (-1, -1, 8 | 1)]
ISO_NEIGHBOR_STEPS = [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)]
ISO_DIAGONAL_STEPS = [(1, -1, 1 | 2), (1, 1, 2 | 4), (-1, 1, 4 | 8), (-1, -1, 8 | 1)]


MASK_TO_KEY = {
    1: "N",
    2: "E",
    4: "S",
    8: "W",
    3: "NE",
    9: "NW",
    6: "SE",
    12: "SW",
    11: "WNE",
    7: "NES",
    13: "SWN",
    14: "ESW",
    15: "NESW",
}


@dataclass(frozen=True)
class Group:
    priority: int


@dataclass
class Style:
    style_id: str
    group_id: str
    base_template: int
    transitions: dict[str, dict[str, int]]


@dataclass
class AutoTileRules:
    groups: dict[str, Group]
    styles: dict[str, Style]
    style_by_template: dict[int, Style]
    base_templates_by_group: dict[str, set[int]]
    transition_groups_by_template: dict[int, set[str]]
    edge_masks_by_template: dict[int, dict[str, int]]
    ignore_transition_neighbors: bool
    use_higher_priority: bool
    invert_mask: bool
    include_diagonals: bool
    allow_enclosed_transitions: bool
    ignore_non_base_templates: bool


def parse_autotile_rules(autotile_yaml: Path) -> AutoTileRules:
    # OpenRA MiniYaml allows tabs; convert to spaces for PyYAML.
    text = autotile_yaml.read_text(encoding="utf-8").replace("\t", "    ")
    data = yaml.safe_load(text)
    info = data.get("EditorWorld", {}).get("AutoTile") or data.get("World", {}).get("AutoTile")
    if not info:
        raise SystemExit("AutoTile rules not found in autotile.yaml")

    groups = {k: Group(int(v["Priority"])) for k, v in info.get("Groups", {}).items()}

    styles: dict[str, Style] = {}
    style_by_template: dict[int, Style] = {}
    base_templates_by_group: dict[str, set[int]] = {}
    transition_groups_by_template: dict[int, set[str]] = {}
    edge_masks_by_template: dict[int, dict[str, int]] = {}

    def add_transition_group(template_id: int, group_id: str) -> None:
        if template_id == 0:
            return
        transition_groups_by_template.setdefault(template_id, set()).add(group_id)

    def add_edge_mask(template_id: int, group_id: str, mask: int, intersect: bool = False) -> None:
        if template_id == 0:
            return
        masks_by_group = edge_masks_by_template.setdefault(template_id, {})
        if group_id not in masks_by_group:
            masks_by_group[group_id] = mask
        else:
            masks_by_group[group_id] = (masks_by_group[group_id] & mask) if intersect else (masks_by_group[group_id] | mask)

    all_edges = 1 | 2 | 4 | 8

    def add_transition_edge_masks(transitions: dict[str, int], base_group_id: str, neighbor_group_id: str) -> None:
        def add_transition(template_id: int, mask: int) -> None:
            if template_id == 0:
                return
            add_edge_mask(template_id, neighbor_group_id, mask)
            add_edge_mask(template_id, base_group_id, all_edges & ~mask, intersect=True)

        add_transition(transitions.get("N", 0), 1)
        add_transition(transitions.get("E", 0), 2)
        add_transition(transitions.get("S", 0), 4)
        add_transition(transitions.get("W", 0), 8)
        add_transition(transitions.get("NE", 0), 1 | 2)
        add_transition(transitions.get("NW", 0), 1 | 8)
        add_transition(transitions.get("SE", 0), 4 | 2)
        add_transition(transitions.get("SW", 0), 4 | 8)
        add_transition(transitions.get("WNE", 0), 8 | 1 | 2)
        add_transition(transitions.get("NES", 0), 1 | 2 | 4)
        add_transition(transitions.get("SWN", 0), 4 | 8 | 1)
        add_transition(transitions.get("ESW", 0), 2 | 4 | 8)
        add_transition(transitions.get("NESW", 0), all_edges)
        add_transition(transitions.get("Hole", 0), all_edges)
        add_transition(transitions.get("U_NW", 0), all_edges)
        add_transition(transitions.get("U_NE", 0), all_edges)
        add_transition(transitions.get("U_SE", 0), all_edges)
        add_transition(transitions.get("U_SW", 0), all_edges)
        add_transition(transitions.get("Parallel_NE_SW", 0), all_edges)
        add_transition(transitions.get("Parallel_NW_SE", 0), all_edges)
        add_transition(transitions.get("End_NW", 0), 1 | 8)
        add_transition(transitions.get("End_NE", 0), 1 | 2)
        add_transition(transitions.get("End_SE", 0), 4 | 2)
        add_transition(transitions.get("End_SW", 0), 4 | 8)
    for style_id, sval in info.get("Styles", {}).items():
        style = Style(
            style_id=style_id,
            group_id=sval["Group"],
            base_template=int(sval["BaseTemplate"]),
            transitions=sval.get("Transitions", {}) or {},
        )
        styles[style_id] = style
        style_by_template[style.base_template] = style
        base_templates_by_group.setdefault(style.group_id, set()).add(style.base_template)
        add_edge_mask(style.base_template, style.group_id, all_edges)
        for tvals in style.transitions.values():
            for _, tid in tvals.items():
                if isinstance(tid, int) and tid != 0:
                    style_by_template[tid] = style
                    add_transition_group(tid, style.group_id)

        for neighbor_group_id, transitions in style.transitions.items():
            if neighbor_group_id not in groups:
                continue
            for tid in transitions.values():
                if isinstance(tid, int) and tid != 0:
                    add_transition_group(tid, neighbor_group_id)
            add_transition_edge_masks(transitions, style.group_id, neighbor_group_id)

    return AutoTileRules(
        groups=groups,
        styles=styles,
        style_by_template=style_by_template,
        base_templates_by_group=base_templates_by_group,
        transition_groups_by_template=transition_groups_by_template,
        edge_masks_by_template=edge_masks_by_template,
        ignore_transition_neighbors=bool(info.get("IgnoreTransitionNeighbors", False)),
        use_higher_priority=bool(info.get("UseHigherPriority", False)),
        invert_mask=bool(info.get("InvertMask", False)),
        include_diagonals=bool(info.get("IncludeDiagonals", False)),
        allow_enclosed_transitions=bool(info.get("AllowEnclosedTransitions", False)),
        ignore_non_base_templates=bool(info.get("IgnoreNonBaseTemplates", False)),
    )


def parse_grid_type(mod_yaml: Path) -> str:
    if not mod_yaml.exists():
        return "Rectangular"
    text = mod_yaml.read_text(encoding="utf-8", errors="replace")
    m = re.search(r"^\s*Type:\s*(\w+)\s*$", text, flags=re.MULTILINE)
    if not m:
        return "Rectangular"
    return m.group(1)


def load_pattern_map_from_rules(
    rules: AutoTileRules,
    group_id: str,
    default_radius: int,
) -> tuple[dict[tuple[bool, ...], int] | None, list[tuple[int, int]] | None, int | None, bool]:
    for style in rules.styles.values():
        transitions = style.transitions.get(group_id)
        if not transitions:
            continue
        raw_map = transitions.get("PatternMap")
        if not isinstance(raw_map, dict):
            continue
        pattern_only = bool(transitions.get("PatternOnly", False))
        radius = transitions.get("PatternRadius", default_radius)
        try:
            radius = int(radius)
        except (TypeError, ValueError):
            radius = default_radius
        if radius <= 0:
            continue
        offsets = [
            (du, dv)
            for dv in range(-radius, radius + 1)
            for du in range(-radius, radius + 1)
            if not (du == 0 and dv == 0)
        ]
        expected_len = len(offsets)
        pattern_map: dict[tuple[bool, ...], int] = {}
        for key, value in raw_map.items():
            if isinstance(key, int):
                dec = str(key)
                octal = format(key, "o")
                dec_is_binary = all(ch in "01" for ch in dec)
                oct_is_binary = all(ch in "01" for ch in octal)
                if dec_is_binary and not oct_is_binary:
                    key_str = dec
                elif oct_is_binary and not dec_is_binary:
                    key_str = octal
                elif oct_is_binary:
                    key_str = octal if len(octal) >= len(dec) else dec
                else:
                    key_str = dec
                key_str = key_str.zfill(expected_len)
            else:
                key_str = str(key).strip()
                if len(key_str) < expected_len and all(ch in "01" for ch in key_str):
                    key_str = key_str.zfill(expected_len)
            if len(key_str) != expected_len:
                continue
            bits = tuple(ch == "1" for ch in key_str)
            try:
                pattern_map[bits] = int(value)
            except (TypeError, ValueError):
                continue
        if pattern_map:
            return pattern_map, offsets, radius, pattern_only
    return None, None, None, False


def cpos_to_mpos(x: int, y: int, grid_type: str) -> tuple[int, int]:
    if grid_type != "RectangularIsometric":
        return (x, y)
    u = (x - y) // 2
    v = x + y
    return (u, v)


def mpos_to_cpos(u: int, v: int, grid_type: str) -> tuple[int, int]:
    if grid_type != "RectangularIsometric":
        return (u, v)
    offset = 1 if (v & 1) == 1 else 0
    y = (v - offset) // 2 - u
    x = v - y
    return (x, y)


def neighbor_dirs_for_cell(x: int, y: int, grid_type: str) -> list[tuple[int, int, int]]:
    if grid_type != "RectangularIsometric":
        return [(x + dx, y + dy, mask) for dx, dy, mask in RECT_NEIGHBOR_STEPS]
    return [(x + dx, y + dy, mask) for dx, dy, mask in RECT_NEIGHBOR_STEPS]


def diagonal_dirs_for_cell(x: int, y: int, grid_type: str) -> list[tuple[int, int, int]]:
    if grid_type != "RectangularIsometric":
        return [(x + dx, y + dy, mask) for dx, dy, mask in RECT_DIAGONAL_STEPS]
    return [(x + dx, y + dy, mask) for dx, dy, mask in RECT_DIAGONAL_STEPS]


def offsets_by_mask(x: int, y: int, dirs: list[tuple[int, int, int]]) -> dict[int, tuple[int, int]]:
    offsets: dict[int, tuple[int, int]] = {}
    for nx, ny, mask in dirs:
        offsets[mask] = (nx - x, ny - y)
    return offsets


def build_group_membership(rules: AutoTileRules) -> dict[int, set[str]]:
    groups_by_template: dict[int, set[str]] = {}
    for template_id, style in rules.style_by_template.items():
        groups_by_template.setdefault(template_id, set()).add(style.group_id)
    for template_id, groups in rules.transition_groups_by_template.items():
        groups_by_template.setdefault(template_id, set()).update(groups)
    return groups_by_template


def read_map_tiles(map_path: Path) -> tuple[list[list[int]], list[list[int]], bytearray, int, int, int]:
    with zipfile.ZipFile(map_path) as z:
        yaml_text = z.read("map.yaml").decode("utf-8", errors="replace")
        data = bytearray(z.read("map.bin"))

    m = re.search(r"MapSize:\s*(\d+)\s*,\s*(\d+)", yaml_text)
    if not m:
        raise SystemExit("MapSize not found in map.yaml")
    width, height = map(int, m.groups())

    fmt = data[0]
    width2, height2 = struct.unpack_from("<HH", data, 1)
    if (width2, height2) != (width, height):
        raise SystemExit("Map size mismatch between map.yaml and map.bin")

    if fmt == 1:
        tiles_off = 5
    elif fmt == 2:
        tiles_off = struct.unpack_from("<I", data, 5)[0]
    else:
        raise SystemExit(f"Unknown map.bin format {fmt}")

    ids = [[0] * height for _ in range(width)]
    indices = [[0] * height for _ in range(width)]
    idx = tiles_off
    for x in range(width):
        for y in range(height):
            ids[x][y] = struct.unpack_from("<H", data, idx)[0]
            indices[x][y] = data[idx + 2]
            idx += 3

    return ids, indices, data, width, height, tiles_off


def mirror_marker_windows(
    ids: list[list[int]],
    indices: list[list[int]],
    expected_marker_id: int,
    actual_marker_id: int,
    window: int,
) -> tuple[list[list[int]], list[list[int]], int, int]:
    width = len(ids)
    height = len(ids[0]) if width else 0
    expected_markers_all = sorted(
        [(x, y) for x in range(width) for y in range(height) if ids[x][y] == expected_marker_id],
        key=lambda p: (p[0], p[1]),
    )
    actual_markers_all = sorted(
        [(x, y) for x in range(width) for y in range(height) if ids[x][y] == actual_marker_id],
        key=lambda p: (p[0], p[1]),
    )

    if not expected_markers_all:
        raise SystemExit(f"No marker tiles with id {expected_marker_id} found")
    if not actual_markers_all:
        raise SystemExit(f"No marker tiles with id {actual_marker_id} found")

    def select_main_row(markers: list[tuple[int, int]]) -> tuple[list[tuple[int, int]], int]:
        counts: dict[int, int] = {}
        for _, y in markers:
            counts[y] = counts.get(y, 0) + 1
        # Prefer the densest row; break ties by choosing the northern row.
        row_y = sorted(counts.items(), key=lambda kv: (-kv[1], kv[0]))[0][0]
        row = sorted([(x, y) for x, y in markers if y == row_y], key=lambda p: p[0])
        return row, row_y

    expected_markers, expected_row_y = select_main_row(expected_markers_all)
    actual_markers, actual_row_y = select_main_row(actual_markers_all)

    radius = max(0, int(window))
    split_y = (expected_row_y + actual_row_y) // 2
    out = [[ids[x][y] for y in range(height)] for x in range(width)]
    out_indices = [[indices[x][y] for y in range(height)] for x in range(width)]
    changed = 0
    actual_by_x = {x: (x, y) for x, y in actual_markers}

    pairs: list[tuple[tuple[int, int], tuple[int, int]]] = []
    for ex, ey in expected_markers:
        pairs.append(((ex, ey), actual_by_x.get(ex, (ex, actual_row_y))))

    for (_, _), (ax, ay) in pairs:
        if 0 <= ax < width and 0 <= ay < height and ay > split_y and out[ax][ay] != actual_marker_id:
            out[ax][ay] = actual_marker_id
            changed += 1

    for (ex, ey), (ax, ay) in pairs:
        dx, dy = ax - ex, ay - ey
        for oy in range(-radius, radius + 1):
            for ox in range(-radius, radius + 1):
                sx, sy = ex + ox, ey + oy
                tx, ty = sx + dx, sy + dy
                if not (0 <= sx < width and 0 <= sy < height and 0 <= tx < width and 0 <= ty < height):
                    continue
                if ty <= split_y:
                    continue

                # Keep actual-row marker tiles untouched.
                if out[tx][ty] == actual_marker_id:
                    continue

                v = ids[sx][sy]
                vi = indices[sx][sy]
                if v == expected_marker_id or v == actual_marker_id:
                    continue
                if out[tx][ty] != v or out_indices[tx][ty] != vi:
                    out[tx][ty] = v
                    out_indices[tx][ty] = vi
                    changed += 1

    if len(expected_markers) != len(actual_markers):
        print(
            f"warning: marker rows differ (expected-row y={expected_row_y} count={len(expected_markers)}, "
            f"actual-row y={actual_row_y} count={len(actual_markers)}); filled missing pairs by x"
        )

    return out, out_indices, changed, len(pairs)


def mirror_marker_band(
    ids: list[list[int]],
    indices: list[list[int]],
    expected_marker_id: int,
    actual_marker_id: int,
    margin_x: int,
    margin_y: int,
) -> tuple[list[list[int]], list[list[int]], int, int]:
    width = len(ids)
    height = len(ids[0]) if width else 0
    expected_markers_all = sorted(
        [(x, y) for x in range(width) for y in range(height) if ids[x][y] == expected_marker_id],
        key=lambda p: (p[0], p[1]),
    )
    actual_markers_all = sorted(
        [(x, y) for x in range(width) for y in range(height) if ids[x][y] == actual_marker_id],
        key=lambda p: (p[0], p[1]),
    )

    if not expected_markers_all:
        raise SystemExit(f"No marker tiles with id {expected_marker_id} found")
    if not actual_markers_all:
        raise SystemExit(f"No marker tiles with id {actual_marker_id} found")

    def select_main_row(markers: list[tuple[int, int]]) -> tuple[list[tuple[int, int]], int]:
        counts: dict[int, int] = {}
        for _, y in markers:
            counts[y] = counts.get(y, 0) + 1
        row_y = sorted(counts.items(), key=lambda kv: (-kv[1], kv[0]))[0][0]
        row = sorted([(x, y) for x, y in markers if y == row_y], key=lambda p: p[0])
        return row, row_y

    expected_markers, expected_row_y = select_main_row(expected_markers_all)
    actual_markers, actual_row_y = select_main_row(actual_markers_all)
    actual_by_x = {x: (x, y) for x, y in actual_markers}

    pairs: list[tuple[tuple[int, int], tuple[int, int]]] = []
    for ex, ey in expected_markers:
        pairs.append(((ex, ey), actual_by_x.get(ex, (ex, actual_row_y))))

    split_y = (expected_row_y + actual_row_y) // 2
    out = [[ids[x][y] for y in range(height)] for x in range(width)]
    out_indices = [[indices[x][y] for y in range(height)] for x in range(width)]
    changed = 0
    for (_, _), (ax, ay) in pairs:
        if 0 <= ax < width and 0 <= ay < height and ay > split_y and out[ax][ay] != actual_marker_id:
            out[ax][ay] = actual_marker_id
            changed += 1

    rx = max(0, int(margin_x))
    ry = max(0, int(margin_y))
    min_x = min(ex for (ex, _), _ in pairs) - rx
    max_x = max(ex for (ex, _), _ in pairs) + rx
    min_y = expected_row_y - ry
    max_y = expected_row_y + ry

    for sx in range(min_x, max_x + 1):
        for sy in range(min_y, max_y + 1):
            if not (0 <= sx < width and 0 <= sy < height):
                continue

            (ex, ey), (ax, ay) = min(pairs, key=lambda p: abs(sx - p[0][0]))
            dx, dy = ax - ex, ay - ey
            tx, ty = sx + dx, sy + dy
            if not (0 <= tx < width and 0 <= ty < height):
                continue
            if ty <= split_y:
                continue

            if out[tx][ty] == actual_marker_id:
                continue

            v = ids[sx][sy]
            vi = indices[sx][sy]
            if v == expected_marker_id or v == actual_marker_id:
                continue
            if out[tx][ty] != v or out_indices[tx][ty] != vi:
                out[tx][ty] = v
                out_indices[tx][ty] = vi
                changed += 1

    if len(expected_markers) != len(actual_markers):
        print(
            f"warning: marker rows differ (expected-row y={expected_row_y} count={len(expected_markers)}, "
            f"actual-row y={actual_row_y} count={len(actual_markers)}); filled missing pairs by x"
        )

    return out, out_indices, changed, len(pairs)


def collect_marker_patches(
    ids: list[list[int]],
    rules: AutoTileRules,
    grid_type: str,
    marker_id: int,
    group_id: str,
    marker_radius: int,
    padding: int,
    window: int | None,
) -> tuple[list[tuple[int, int]], list[set[tuple[int, int]]], set[tuple[int, int]]]:
    width = len(ids)
    height = len(ids[0]) if width else 0
    base_templates = rules.base_templates_by_group.get(group_id, set())
    if not base_templates:
        raise SystemExit(f"No base templates found for group '{group_id}'")

    markers = [(x, y) for x in range(width) for y in range(height) if ids[x][y] == marker_id]
    if not markers:
        raise SystemExit(f"No marker tiles with id {marker_id} found")

    patches: list[set[tuple[int, int]]] = []
    union_cells: set[tuple[int, int]] = set()

    for mx, my in markers:
        cells: set[tuple[int, int]] = set()
        if window is not None:
            w = max(0, window)
            for x in range(max(0, mx - w), min(width, mx + w + 1)):
                for y in range(max(0, my - w), min(height, my + w + 1)):
                    cells.add((x, y))
        else:
            best = None
            best_dist = None
            r = marker_radius
            for dx in range(-r, r + 1):
                for dy in range(-r, r + 1):
                    x, y = mx + dx, my + dy
                    if 0 <= x < width and 0 <= y < height and ids[x][y] in base_templates:
                        dist = abs(dx) + abs(dy)
                        if best_dist is None or dist < best_dist:
                            best = (x, y)
                            best_dist = dist

            if best is None:
                patches.append(cells)
                continue

            stack = [best]
            seen = set()
            minx = miny = 10**9
            maxx = maxy = -10**9
            while stack:
                x, y = stack.pop()
                if (x, y) in seen:
                    continue
                seen.add((x, y))
                if ids[x][y] not in base_templates:
                    continue
                minx = min(minx, x)
                maxx = max(maxx, x)
                miny = min(miny, y)
                maxy = max(maxy, y)
                for nx, ny, _ in neighbor_dirs_for_cell(x, y, grid_type):
                    if 0 <= nx < width and 0 <= ny < height and (nx, ny) not in seen:
                        stack.append((nx, ny))

            if minx <= maxx and miny <= maxy:
                pad = max(0, padding)
                for x in range(max(0, minx - pad), min(width, maxx + pad + 1)):
                    for y in range(max(0, miny - pad), min(height, maxy + pad + 1)):
                        cells.add((x, y))

        patches.append(cells)
        union_cells.update(cells)

    return markers, patches, union_cells


def apply_mask_direction_overrides(mask: int, invert_mask: bool) -> int:
    if not invert_mask:
        return mask
    inverted = 0
    if mask & 1:
        inverted |= 4
    if mask & 4:
        inverted |= 1
    if mask & 2:
        inverted |= 8
    if mask & 8:
        inverted |= 2
    return inverted


def resolve_all(
    ids: list[list[int]],
    rules: AutoTileRules,
    update_cells: set[tuple[int, int]] | None,
    grid_type: str,
    pattern_map: dict[tuple[bool, ...], int] | None = None,
    pattern_offsets: list[tuple[int, int]] | None = None,
    pattern_group_id: str | None = None,
    pattern_only: bool = False,
    iterations: int = 1,
) -> list[list[int]]:
    width = len(ids)
    height = len(ids[0]) if width else 0
    current_ids = [[ids[x][y] for y in range(height)] for x in range(width)]

    def contains(x: int, y: int) -> bool:
        return 0 <= x < width and 0 <= y < height

    def is_normal_priority(nei_p: int, grp_p: int) -> bool:
        return nei_p > grp_p if rules.use_higher_priority else nei_p < grp_p

    def is_opposite_priority(nei_p: int, grp_p: int) -> bool:
        return nei_p < grp_p if rules.use_higher_priority else nei_p > grp_p

    def is_base_template_in_group(x: int, y: int, group_id: str) -> bool:
        if not contains(x, y):
            return False
        tid = current_ids[x][y]
        st = rules.style_by_template.get(tid)
        if not st:
            return False
        return st.group_id == group_id and tid == st.base_template

    def is_enclosed_by_group(x: int, y: int, enclosing_group_id: str) -> bool:
        for nx, ny, _ in neighbor_dirs_for_cell(x, y, grid_type):
            if not contains(nx, ny):
                return False
            tid = current_ids[nx][ny]
            st = rules.style_by_template.get(tid)
            if not st or st.group_id != enclosing_group_id:
                return False
        return True

    def is_base_neighbor_group(
        x: int,
        y: int,
        style: Style,
        neighbor_group: Group,
        group_priority: int,
        neighbor_group_id: str,
    ) -> bool:
        if not is_base_template_in_group(x, y, neighbor_group_id):
            return False
        if is_normal_priority(neighbor_group.priority, group_priority):
            return True
        return rules.allow_enclosed_transitions and is_opposite_priority(neighbor_group.priority, group_priority) and is_enclosed_by_group(x, y, style.group_id)

    def has_transition_anchor(x: int, y: int, group_id: str, mask: int) -> bool:
        if mask == 0:
            return False
        neighbors = {m: (nx, ny) for nx, ny, m in neighbor_dirs_for_cell(x, y, grid_type)}
        if (mask & 1) != 0:
            nx, ny = neighbors.get(1, (None, None))
            if nx is not None and is_base_template_in_group(nx, ny, group_id):
                return True
        if (mask & 2) != 0:
            nx, ny = neighbors.get(2, (None, None))
            if nx is not None and is_base_template_in_group(nx, ny, group_id):
                return True
        if (mask & 4) != 0:
            nx, ny = neighbors.get(4, (None, None))
            if nx is not None and is_base_template_in_group(nx, ny, group_id):
                return True
        if (mask & 8) != 0:
            nx, ny = neighbors.get(8, (None, None))
            if nx is not None and is_base_template_in_group(nx, ny, group_id):
                return True
        return False

    def is_neighbor_group_tile(
        x: int,
        y: int,
        style: Style,
        neighbor_group: Group,
        group_priority: int,
        neighbor_group_id: str,
        allow_transitions: bool,
    ) -> bool:
        if not contains(x, y):
            return False
        tid = current_ids[x][y]
        neighbor_style = rules.style_by_template.get(tid)
        if not neighbor_style:
            return False
        if (not allow_transitions) and rules.ignore_transition_neighbors and tid != neighbor_style.base_template:
            return False

        mask = 0
        has_mask = False
        masks_by_group = rules.edge_masks_by_template.get(tid)
        if masks_by_group and neighbor_group_id in masks_by_group:
            mask = masks_by_group[neighbor_group_id]
            has_mask = True
        else:
            if neighbor_style.group_id != neighbor_group_id:
                return False

        if allow_transitions and has_mask and tid != neighbor_style.base_template:
            if not has_transition_anchor(x, y, neighbor_group_id, mask):
                return False

        if is_normal_priority(neighbor_group.priority, group_priority):
            return True

        return rules.allow_enclosed_transitions and is_opposite_priority(neighbor_group.priority, group_priority) and is_enclosed_by_group(x, y, style.group_id)

    def is_neighbor_group_tile_anchored(
        x: int,
        y: int,
        style: Style,
        neighbor_group: Group,
        group_priority: int,
        neighbor_group_id: str,
    ) -> bool:
        return is_neighbor_group_tile(x, y, style, neighbor_group, group_priority, neighbor_group_id, allow_transitions=True)

    def has_neighbor_group_edge(
        x: int,
        y: int,
        style: Style,
        neighbor_group: Group,
        group_priority: int,
        neighbor_group_id: str,
        required_mask: int,
    ) -> bool:
        if not contains(x, y):
            return False
        tid = current_ids[x][y]
        neighbor_style = rules.style_by_template.get(tid)
        if not neighbor_style:
            return False

        mask = 0
        has_mask = False
        masks_by_group = rules.edge_masks_by_template.get(tid)
        if masks_by_group and neighbor_group_id in masks_by_group:
            mask = masks_by_group[neighbor_group_id]
            has_mask = True
            if required_mask != 0 and (mask & required_mask) != required_mask:
                return False
        else:
            if neighbor_style.group_id != neighbor_group_id:
                return False

        if has_mask and tid != neighbor_style.base_template:
            if not has_transition_anchor(x, y, neighbor_group_id, mask):
                return False

        if is_normal_priority(neighbor_group.priority, group_priority):
            return True

        return rules.allow_enclosed_transitions and is_opposite_priority(neighbor_group.priority, group_priority) and is_enclosed_by_group(x, y, style.group_id)

    def has_transitions_for(style: Style, neighbor_group_id: str, neighbor_style_id: str | None) -> bool:
        if neighbor_style_id is not None and neighbor_style_id in style.transitions:
            return True
        return neighbor_group_id in style.transitions

    def try_find_neighbor_group(
        x: int, y: int, style: Style, group_priority: int
    ) -> tuple[str | None, str | None]:
        selected_priority = -10**9
        selected_group_id = None
        selected_style_id = None

        for nx, ny, _ in neighbor_dirs_for_cell(x, y, grid_type):
            if not contains(nx, ny):
                continue
            tid = current_ids[nx][ny]
            neighbor_style = rules.style_by_template.get(tid)
            if not neighbor_style:
                continue
            def consider_group(candidate_group_id: str, candidate_style_id: str | None) -> None:
                nonlocal selected_priority, selected_group_id, selected_style_id
                neighbor_group = rules.groups.get(candidate_group_id)
                if not neighbor_group:
                    return
                if rules.allow_enclosed_transitions and not has_transitions_for(style, candidate_group_id, candidate_style_id):
                    return
                if not has_neighbor_group_edge(nx, ny, style, neighbor_group, group_priority, candidate_group_id, 0):
                    return
                if neighbor_group.priority <= selected_priority:
                    return
                selected_priority = neighbor_group.priority
                selected_group_id = candidate_group_id
                selected_style_id = candidate_style_id

            base_style_id = neighbor_style.style_id if tid == neighbor_style.base_template else None
            consider_group(neighbor_style.group_id, base_style_id)

            transition_groups = rules.transition_groups_by_template.get(tid)
            if transition_groups:
                for candidate_group_id in transition_groups:
                    if candidate_group_id == neighbor_style.group_id:
                        continue
                    consider_group(candidate_group_id, None)

        return selected_group_id, selected_style_id

    def try_resolve_transitions(style: Style, neighbor_group_id: str, neighbor_style_id: str | None) -> dict[str, int] | None:
        if neighbor_style_id is not None and neighbor_style_id in style.transitions:
            return style.transitions[neighbor_style_id]
        return style.transitions.get(neighbor_group_id)

    def try_resolve_special_template(
        x: int,
        y: int,
        style: Style,
        group_priority: int,
        neighbor_group_id: str,
        transitions: dict[str, int],
        base_only: bool,
    ) -> int | None:
        neighbor_group = rules.groups.get(neighbor_group_id)
        if not neighbor_group:
            return None

        neighbor_offsets = offsets_by_mask(x, y, neighbor_dirs_for_cell(x, y, grid_type))
        diagonal_offsets = offsets_by_mask(x, y, diagonal_dirs_for_cell(x, y, grid_type))

        def edge_at(offset: tuple[int, int] | None, required: int) -> bool:
            if offset is None:
                return False
            dx, dy = offset
            if base_only:
                return is_base_neighbor_group(x + dx, y + dy, style, neighbor_group, group_priority, neighbor_group_id)
            return has_neighbor_group_edge(x + dx, y + dy, style, neighbor_group, group_priority, neighbor_group_id, required)

        nT = edge_at(neighbor_offsets.get(1), 4)   # south edge on northern neighbor
        eT = edge_at(neighbor_offsets.get(2), 8)   # west edge on eastern neighbor
        sT = edge_at(neighbor_offsets.get(4), 1)   # north edge on southern neighbor
        wT = edge_at(neighbor_offsets.get(8), 2)   # east edge on western neighbor
        neT = edge_at(diagonal_offsets.get(1 | 2), 4 | 8)
        nwT = edge_at(diagonal_offsets.get(1 | 8), 4 | 2)
        seT = edge_at(diagonal_offsets.get(4 | 2), 1 | 8)
        swT = edge_at(diagonal_offsets.get(4 | 8), 1 | 2)
        all_orth = nT and eT and sT and wT

        if transitions.get("Hole", 0) and all_orth and neT and nwT and seT and swT:
            return transitions["Hole"]

        if transitions.get("WNE", 0) and nT and wT and eT and not sT and nwT and neT and swT and not seT:
            return transitions["WNE"]
        if transitions.get("NES", 0) and nT and eT and not wT and not sT and neT and nwT and seT and not swT:
            return transitions["NES"]
        if transitions.get("SWN", 0) and sT and wT and not nT and not eT and swT and nwT and seT and not neT:
            return transitions["SWN"]
        if transitions.get("ESW", 0) and sT and eT and not nT and not wT and seT and neT and swT and not nwT:
            return transitions["ESW"]

        if transitions.get("End_NW", 0) and nT and wT and not eT and not sT and not nwT:
            return transitions["End_NW"]
        if transitions.get("End_NE", 0) and nT and eT and not sT and not wT and not neT:
            return transitions["End_NE"]
        if transitions.get("End_SE", 0) and sT and eT and not nT and not wT and not seT:
            return transitions["End_SE"]
        if transitions.get("End_SW", 0) and sT and wT and not nT and not eT and not swT:
            return transitions["End_SW"]

        if all_orth:
            if transitions.get("U_NW", 0) and not nwT and neT and seT and swT:
                return transitions["U_NW"]
            if transitions.get("U_NE", 0) and not neT and nwT and seT and swT:
                return transitions["U_NE"]
            if transitions.get("U_SE", 0) and not seT and neT and nwT and swT:
                return transitions["U_SE"]
            if transitions.get("U_SW", 0) and not swT and neT and nwT and seT:
                return transitions["U_SW"]
            if transitions.get("Parallel_NE_SW", 0) and neT and swT and not nwT and not seT:
                return transitions["Parallel_NE_SW"]
            if transitions.get("Parallel_NW_SE", 0) and nwT and seT and not neT and not swT:
                return transitions["Parallel_NW_SE"]

        return None

    def build_mask(x: int, y: int, style: Style, group_priority: int, neighbor_group_id: str, base_only: bool) -> int:
        mask = 0
        neighbor_group = rules.groups.get(neighbor_group_id)
        if not neighbor_group:
            return mask

        for nx, ny, bit in neighbor_dirs_for_cell(x, y, grid_type):
            required = 0
            if bit == 1:
                required = 4
            elif bit == 2:
                required = 8
            elif bit == 4:
                required = 1
            elif bit == 8:
                required = 2
            if base_only:
                if is_base_neighbor_group(nx, ny, style, neighbor_group, group_priority, neighbor_group_id):
                    mask |= bit
                    continue
            if has_neighbor_group_edge(nx, ny, style, neighbor_group, group_priority, neighbor_group_id, required):
                mask |= bit

        if rules.include_diagonals:
            for nx, ny, bit in diagonal_dirs_for_cell(x, y, grid_type):
                if mask & bit:
                    continue
                required = 0
                if bit == (1 | 2):
                    required = 4 | 8
                elif bit == (1 | 8):
                    required = 4 | 2
                elif bit == (4 | 2):
                    required = 1 | 8
                elif bit == (4 | 8):
                    required = 1 | 2
                if base_only:
                    if is_base_neighbor_group(nx, ny, style, neighbor_group, group_priority, neighbor_group_id):
                        mask |= bit
                        continue
                if has_neighbor_group_edge(nx, ny, style, neighbor_group, group_priority, neighbor_group_id, required):
                    mask |= bit

        return apply_mask_direction_overrides(mask, rules.invert_mask)

    def resolve_template(x: int, y: int) -> int:
        tid = current_ids[x][y]
        style = rules.style_by_template.get(tid)
        if not style:
            return tid
        if rules.ignore_non_base_templates and tid != style.base_template:
            return tid

        group = rules.groups.get(style.group_id)
        if not group:
            return style.base_template

        base_only = False
        neighbor_group_id = None
        neighbor_style_id = None

        # Prefer pattern-only groups based on base neighbors.
        for candidate_group_id, candidate_transitions in style.transitions.items():
            if not candidate_transitions or not candidate_transitions.get("PatternOnly", False):
                continue
            neighbor_group = rules.groups.get(candidate_group_id)
            if not neighbor_group:
                continue
            if any(
                is_base_neighbor_group(nx, ny, style, neighbor_group, group.priority, candidate_group_id)
                for nx, ny, _ in neighbor_dirs_for_cell(x, y, grid_type)
            ):
                neighbor_group_id = candidate_group_id
                base_only = True
                break

        if neighbor_group_id is None:
            neighbor_group_id, neighbor_style_id = try_find_neighbor_group(x, y, style, group.priority)
            if not neighbor_group_id:
                return style.base_template

        transitions = try_resolve_transitions(style, neighbor_group_id, neighbor_style_id)
        if not transitions:
            return style.base_template

        if transitions.get("PatternOnly", False):
            base_only = True

        if pattern_map and neighbor_group_id == pattern_group_id:
            key = pattern_for_cell(x, y)
            if key is not None and key in pattern_map:
                return pattern_map[key]
            if pattern_only:
                return tid

        special = try_resolve_special_template(x, y, style, group.priority, neighbor_group_id, transitions, base_only)
        if special:
            return special

        mask = build_mask(x, y, style, group.priority, neighbor_group_id, base_only)
        if mask == 0:
            return style.base_template

        if rules.allow_enclosed_transitions and mask == 15:
            neighbor_group = rules.groups.get(neighbor_group_id)
            if neighbor_group and is_normal_priority(neighbor_group.priority, group.priority):
                if is_enclosed_by_group(x, y, neighbor_group_id):
                    return style.base_template

        key = MASK_TO_KEY.get(mask)
        if key and key in transitions and transitions[key] != 0:
            return transitions[key]

        return style.base_template

    base_group_by_cell = None
    def rebuild_base_group_cache() -> None:
        nonlocal base_group_by_cell
        if not (pattern_map and pattern_offsets and pattern_group_id):
            base_group_by_cell = None
            return

        base_group_by_cell = [[None for _ in range(height)] for _ in range(width)]
        for x in range(width):
            for y in range(height):
                tid = current_ids[x][y]
                style = rules.style_by_template.get(tid)
                if style and tid == style.base_template:
                    base_group_by_cell[x][y] = style.group_id

    def pattern_for_cell(x: int, y: int) -> tuple[bool, ...] | None:
        if not pattern_map or not pattern_offsets or not pattern_group_id:
            return None
        tid = current_ids[x][y]
        style = rules.style_by_template.get(tid)
        if not style or pattern_group_id not in style.transitions:
            return None
        bits = []
        if grid_type == "RectangularIsometric":
            for du, dv in pattern_offsets:
                nx, ny = x + du, y + dv
                in_group = False
                if 0 <= nx < width and 0 <= ny < height:
                    in_group = base_group_by_cell[nx][ny] == pattern_group_id
                bits.append(in_group)
        else:
            u, v = cpos_to_mpos(x, y, grid_type)
            for du, dv in pattern_offsets:
                nx, ny = mpos_to_cpos(u + du, v + dv, grid_type)
                in_group = False
                if 0 <= nx < width and 0 <= ny < height:
                    in_group = base_group_by_cell[nx][ny] == pattern_group_id
                bits.append(in_group)
        return tuple(bits)

    targets = update_cells if update_cells is not None else {(x, y) for x in range(width) for y in range(height)}
    target_list = sorted(targets)

    for _ in range(max(1, iterations)):
        rebuild_base_group_cache()
        next_ids = [[current_ids[x][y] for y in range(height)] for x in range(width)]
        changed = 0
        for x, y in target_list:
            new_value = None
            if new_value is None:
                new_value = resolve_template(x, y)
            if new_value != current_ids[x][y]:
                next_ids[x][y] = new_value
                changed += 1
        current_ids = next_ids
        if changed == 0:
            break

    return current_ids


def main() -> int:
    parser = argparse.ArgumentParser(description="Reapply AutoTile transitions to a map.")
    parser.add_argument("map", type=Path, help="Path to .oramap")
    parser.add_argument("--dry-run", action="store_true", help="Do not write changes")
    parser.add_argument("--marker-id", type=int, help="Only update patches near this marker tile id")
    parser.add_argument("--pattern-learn-marker", type=int, help="Learn a pattern map from patches near this marker tile id")
    parser.add_argument("--pattern-radius", type=int, default=3, help="Pattern radius in MPos (default: 3)")
    parser.add_argument("--pattern-group", type=str, help="Neighbor group for pattern map (default: Sand)")
    parser.add_argument("--pattern-only", action="store_true", help="Do not fall back to AutoTile when pattern not found")
    parser.add_argument("--group-id", type=str, help="Only consider this AutoTile group (default: Sand when marker-id is set)")
    parser.add_argument("--marker-radius", type=int, default=8, help="Search radius around marker for base tiles")
    parser.add_argument("--padding", type=int, default=1, help="Padding around patch bounds to update")
    parser.add_argument("--window", type=int, help="Square half-size around marker to update (overrides patch selection)")
    parser.add_argument("--iterations", type=int, default=1, help="AutoTile passes to run (default: 1)")
    parser.add_argument("--grid-type", type=str, choices=["Rectangular", "RectangularIsometric"], help="Override grid type")
    parser.add_argument("--verify-marker", action="append", type=int, help="Verify tiles near this marker id (repeatable)")
    parser.add_argument("--match-expected-marker", type=int, help="Copy windows around this marker id to the actual marker row")
    parser.add_argument("--match-actual-marker", type=int, help="Destination marker id for --match-expected-marker")
    parser.add_argument("--match-window", type=int, default=0, help="Half-size of marker window to copy when matching rows")
    parser.add_argument("--match-band-x", type=int, help="Half-width extension around marker row when cloning a full band")
    parser.add_argument("--match-band-y", type=int, help="Half-height extension around marker row when cloning a full band")
    args = parser.parse_args()

    map_path = args.map
    if not map_path.exists():
        raise SystemExit(f"Map not found: {map_path}")

    rules = parse_autotile_rules(Path("mods/ca/rules/autotile.yaml"))
    grid_type = args.grid_type or parse_grid_type(Path("mods/ca/mod.yaml"))
    ids, indices, data, width, height, tiles_off = read_map_tiles(map_path)
    original_ids = [[ids[x][y] for y in range(height)] for x in range(width)]
    original_indices = [[indices[x][y] for y in range(height)] for x in range(width)]
    new_indices = [[indices[x][y] for y in range(height)] for x in range(width)]

    if (args.match_expected_marker is None) != (args.match_actual_marker is None):
        raise SystemExit("Both --match-expected-marker and --match-actual-marker are required together")

    if args.match_expected_marker is not None:
        if args.match_band_x is not None or args.match_band_y is not None:
            if args.match_band_x is None or args.match_band_y is None:
                raise SystemExit("Both --match-band-x and --match-band-y are required together")

            ids, new_indices, copied, marker_pairs = mirror_marker_band(
                ids,
                new_indices,
                args.match_expected_marker,
                args.match_actual_marker,
                args.match_band_x,
                args.match_band_y,
            )
            print(
                f"match markers {args.match_expected_marker}->{args.match_actual_marker}: "
                f"pairs={marker_pairs} band=({max(0, args.match_band_x)},{max(0, args.match_band_y)}) changed={copied}"
            )
        else:
            ids, new_indices, copied, marker_pairs = mirror_marker_windows(
                ids,
                new_indices,
                args.match_expected_marker,
                args.match_actual_marker,
                args.match_window,
            )
            print(
                f"match markers {args.match_expected_marker}->{args.match_actual_marker}: "
                f"pairs={marker_pairs} window={max(0, args.match_window)} changed={copied}"
            )

    pattern_map = None
    pattern_offsets: list[tuple[int, int]] | None = None
    pattern_group_id = args.pattern_group or "Sand"
    pattern_radius = args.pattern_radius
    pattern_only = args.pattern_only
    if args.pattern_learn_marker is not None:
        _, _, learn_cells = collect_marker_patches(
            ids,
            rules,
            grid_type,
            args.pattern_learn_marker,
            pattern_group_id,
            args.marker_radius,
            args.padding,
            args.window,
        )

        radius = max(1, args.pattern_radius)
        pattern_radius = radius
        pattern_offsets = [
            (du, dv)
            for dv in range(-radius, radius + 1)
            for du in range(-radius, radius + 1)
            if not (du == 0 and dv == 0)
        ]

        pattern_map = {}
        conflicts = {}

        base_group_by_cell = [[None for _ in range(height)] for _ in range(width)]
        for x in range(width):
            for y in range(height):
                tid = ids[x][y]
                style = rules.style_by_template.get(tid)
                if style and tid == style.base_template:
                    base_group_by_cell[x][y] = style.group_id

        def in_group_at(u: int, v: int) -> bool:
            cx, cy = mpos_to_cpos(u, v, grid_type)
            if 0 <= cx < width and 0 <= cy < height:
                return base_group_by_cell[cx][cy] == pattern_group_id
            return False

        for x, y in learn_cells:
            tid = ids[x][y]
            style = rules.style_by_template.get(tid)
            if not style or pattern_group_id not in style.transitions:
                continue
            if grid_type == "RectangularIsometric":
                key = tuple(
                    0 <= x + du < width and 0 <= y + dv < height and base_group_by_cell[x + du][y + dv] == pattern_group_id
                    for du, dv in pattern_offsets
                )
            else:
                u, v = cpos_to_mpos(x, y, grid_type)
                key = tuple(in_group_at(u + du, v + dv) for du, dv in pattern_offsets)
            if key in pattern_map and pattern_map[key] != tid:
                conflicts.setdefault(key, set()).update([pattern_map[key], tid])
            else:
                pattern_map[key] = tid

        if conflicts:
            raise SystemExit(f"Pattern conflicts detected: {len(conflicts)}")

    if pattern_map is None:
        pattern_map, pattern_offsets, pattern_radius, yaml_pattern_only = load_pattern_map_from_rules(
            rules, pattern_group_id, pattern_radius
        )
        if yaml_pattern_only:
            pattern_only = True

    update_cells: set[tuple[int, int]] | None = None
    if args.marker_id is not None:
        group_id = args.group_id or "Sand"
        _, _, update_cells = collect_marker_patches(
            ids,
            rules,
            grid_type,
            args.marker_id,
            group_id,
            args.marker_radius,
            args.padding,
            args.window,
        )

    verify_results = []
    verify_union: set[tuple[int, int]] = set()
    if args.verify_marker:
        group_id = args.group_id or "Sand"
        for marker_id in args.verify_marker:
            markers, patches, union_cells = collect_marker_patches(
                ids,
                rules,
                grid_type,
                marker_id,
                group_id,
                args.marker_radius,
                args.padding,
                args.window,
            )
            verify_results.append((marker_id, len(markers), patches))
            verify_union.update(union_cells)

    if verify_union:
        if update_cells is None:
            update_cells = verify_union
        else:
            update_cells = set(update_cells) | verify_union

    if update_cells is not None:
        new_ids = resolve_all(
            ids,
            rules,
            update_cells,
            grid_type,
            pattern_map=pattern_map,
            pattern_offsets=pattern_offsets,
            pattern_group_id=pattern_group_id if pattern_map else None,
            pattern_only=pattern_only,
            iterations=args.iterations,
        )
    else:
        new_ids = ids

    if verify_results:
        for marker_id, marker_count, patches in verify_results:
            total_cells = 0
            mismatches = 0
            for patch in patches:
                total_cells += len(patch)
                for x, y in patch:
                    if new_ids[x][y] != original_ids[x][y] or new_indices[x][y] != original_indices[x][y]:
                        mismatches += 1
            print(f"verify {marker_id}: markers={marker_count} cells={total_cells} mismatches={mismatches}")

        if args.marker_id is None:
            return 0

    changes = 0
    idx = tiles_off
    for x in range(width):
        for y in range(height):
            old_id = original_ids[x][y]
            old_index = original_indices[x][y]
            new_id = new_ids[x][y]
            new_index = new_indices[x][y]
            if new_id != old_id and new_index == old_index:
                new_index = 0

            if new_id != old_id or new_index != old_index:
                changes += 1
                data[idx:idx + 2] = struct.pack("<H", new_id)
                data[idx + 2] = new_index & 0xFF
            idx += 3

    if args.dry_run:
        print(f"{map_path}: {changes} tile(s) would change")
        return 0

    if changes == 0:
        print(f"{map_path}: no changes")
        return 0

    # Rewrite map.bin inside the archive
    with zipfile.ZipFile(map_path, "r") as z:
        files = {name: z.read(name) for name in z.namelist() if name != "map.bin"}
    with zipfile.ZipFile(map_path, "w", compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr("map.bin", data)
        for name, content in files.items():
            z.writestr(name, content)

    print(f"{map_path}: wrote {changes} changed tile(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
