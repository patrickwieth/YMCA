#!/usr/bin/env bash
set -euo pipefail
BLENDER=${BLENDER:-'C:/Program Files/Blender Foundation/Blender 3.6/blender.exe'}
ANGLES=$(python -c 'print(",".join(map(str,range(32))))')
for version in 12 13; do
 attempt="game-v${version}-32-native"
 "$BLENDER" --background --python tools/art/render_sentry_v13_game.py -- --version "$version" --native-32
 previous=game-v12-8
 if [ "$version" = 13 ]; then previous=game-v12-32-native; fi
 python tools/art/compare_sentry_v12.py --attempt "$attempt" --previous-attempt "$previous" --previous-prefix final-turret- --angles "$ANGLES" --label "V${version} native120"
 "$BLENDER" --background --python tools/art/render_sentry_v11_edges.py -- --input-attempt "$attempt" --output-attempt "$attempt/edges" --blend-file render.blend --output-blend edge-study.blend --angles "$ANGLES" --angle-step 11.25
 extra=()
 if [ "$version" = 13 ]; then extra=(--paired-highlights --weapon-tone-reference-attempt game-v12-32-native); fi
 python tools/art/finish_sentry_v11_edges.py --input-attempt "$attempt" --output-attempt "$attempt/edges" --angles "$ANGLES" "${extra[@]}"
 python tools/art/compare_sentry_v12.py --attempt "$attempt" --previous-attempt "$previous" --previous-prefix final-turret- --angles "$ANGLES" --label "V${version} native120" --final
 done
