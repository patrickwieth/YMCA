#!/bin/sh
# Run from the repository root, after fetch-engine.sh.
set -eu
for patch in tools/patches/isometric-selection-decoration.patch tools/patches/legacy-map-safety.patch; do
    if git apply --reverse --check --ignore-space-change "$patch" 2>/dev/null; then
        continue
    fi
    git apply --check --ignore-space-change "$patch"
    git apply --ignore-space-change "$patch"
done
