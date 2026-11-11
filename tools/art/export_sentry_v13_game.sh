#!/usr/bin/env bash
# Run from Game. Keeps all attempt-* studies intact.
set -euo pipefail
BLENDER=${BLENDER:-'C:/Program Files/Blender Foundation/Blender 3.6/blender.exe'}
ANGLES=0,45,90,135,180,225,270,315
for version in 12 13; do
  attempt="game-v${version}-8"
  "$BLENDER" --background --python tools/art/render_sentry_v13_game.py -- --version "$version"
  previous=attempt-12
  if [ "$version" = 13 ]; then previous=game-v12-8; fi
  python tools/art/compare_sentry_v12.py --attempt "$attempt" --previous-attempt "$previous" --previous-prefix final-turret- --angles "$ANGLES" --label "V${version} / full rotation"
  "$BLENDER" --background --python tools/art/render_sentry_v11_edges.py -- --input-attempt "$attempt" --output-attempt "$attempt/edges" --blend-file render.blend --output-blend edge-study.blend --angles "$ANGLES"
  extra=()
  if [ "$version" = 13 ]; then extra=(--paired-highlights --weapon-tone-reference-attempt game-v12-8); fi
  python tools/art/finish_sentry_v11_edges.py --input-attempt "$attempt" --output-attempt "$attempt/edges" --angles "$ANGLES" --after-label "V${version} / ink finish" "${extra[@]}"
  python tools/art/compare_sentry_v12.py --attempt "$attempt" --previous-attempt "$previous" --previous-prefix final-turret- --angles "$ANGLES" --label "V${version} / full rotation" --final
 done
