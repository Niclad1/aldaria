#!/usr/bin/env bash
# Ativa a licença da Unity a partir da variável de ambiente UNITY_LICENSE
# (o conteúdo do arquivo Unity_lic.ulf gerado pelo Unity Hub no seu computador).
#   Windows: C:\ProgramData\Unity\Unity_lic.ulf
#   macOS:   /Library/Application Support/Unity/Unity_lic.ulf
#   Linux:   ~/.local/share/unity3d/Unity/Unity_lic.ulf
set -euo pipefail
UNITY="${UNITY_PATH:-$HOME/unity/editor/Editor/Unity}"
if [ -z "${UNITY_LICENSE:-}" ]; then
  echo "Defina a variável UNITY_LICENSE com o conteúdo do Unity_lic.ulf." >&2
  exit 1
fi
lic=$(mktemp --suffix=.ulf)
printf '%s' "$UNITY_LICENSE" > "$lic"
mkdir -p "$HOME/.local/share/unity3d/Unity"
cp "$lic" "$HOME/.local/share/unity3d/Unity/Unity_lic.ulf"
"$UNITY" -batchmode -nographics -quit -manualLicenseFile "$lic" -logFile - | grep -iE "licen|error" || true
rm -f "$lic"
