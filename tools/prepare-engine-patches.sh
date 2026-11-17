#!/bin/sh
# Run from the repository root, after fetch-engine.sh.
set -eu
patch=tools/patches/isometric-selection-decoration.patch
# The pinned engine already includes AutoTile, sprite anchors and repair docking.
# Apply only the remaining selection/building presentation patch, exactly once.
if git apply --reverse --check --ignore-space-change "$patch" 2>/dev/null; then
    exit 0
fi
git apply --check --ignore-space-change "$patch"
git apply --ignore-space-change "$patch"
