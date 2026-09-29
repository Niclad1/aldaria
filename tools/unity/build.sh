#!/usr/bin/env bash
# Compila o jogo pela linha de comando. Uso: tools/unity/build.sh [WebGL|Windows|Linux|CheckCompile]
set -euo pipefail
ALVO="${1:-WebGL}"
UNITY="${UNITY_PATH:-$HOME/unity/editor/Editor/Unity}"
PROJETO="$(cd "$(dirname "$0")/../.." && pwd)"
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJETO" \
  -executeMethod "Aldaria.EditorTools.Builder.$ALVO" -logFile "$PROJETO/Logs/build-$ALVO.log"
echo "Pronto. Log em Logs/build-$ALVO.log"
