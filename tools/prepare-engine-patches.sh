#!/bin/sh
# Run from the repository root, after fetch-engine.sh.
set -eu
base=tools/patches/isometric-selection-decoration.patch
completion=tools/patches/isometric-runtime-completion.patch
# The completion patch builds on the base patch and changes some of its lines.
if git apply --reverse --check --ignore-space-change "$completion" 2>/dev/null; then
    exit 0
fi
if ! git apply --reverse --check --ignore-space-change "$base" 2>/dev/null; then
    git apply --check --ignore-space-change "$base"
    git apply --ignore-space-change "$base"
fi
git apply --check --ignore-space-change "$completion"
git apply --ignore-space-change "$completion"
