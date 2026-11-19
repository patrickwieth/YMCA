#!/bin/sh
# Run from the repository root, after fetch-engine.sh.
set -eu
while IFS= read -r patch; do
    patch=$(printf '%s' "$patch" | tr -d '\r')
    [ -n "$patch" ] || continue
    if git apply --reverse --check --ignore-space-change "$patch" 2>/dev/null; then
        continue
    fi
    git apply --check --ignore-space-change "$patch"
    git apply --ignore-space-change "$patch"
done < tools/engine-patches.txt
