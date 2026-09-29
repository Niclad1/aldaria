#!/usr/bin/env bash
# Instala a Unity 6 LTS para Linux sem o Unity Hub (útil em servidores e na nuvem).
# Uso: tools/unity/instalar-unity.sh [pasta]   (padrão: ~/unity)
set -euo pipefail
VERSAO="6000.0.84f1"
REVISAO="78ab6fc243d5"
DEST="${1:-$HOME/unity}"
BASE="https://download.unity3d.com/download_unity/$REVISAO"

mkdir -p "$DEST"
cd "$DEST"
if [ ! -x editor/Editor/Unity ]; then
  echo "Baixando Unity $VERSAO (≈4,5 GB)..."
  curl -L --retry 5 -C - -o unity.tar.xz "$BASE/LinuxEditorInstaller/Unity-$VERSAO.tar.xz"
  mkdir -p editor && tar -xJf unity.tar.xz -C editor && rm unity.tar.xz
fi
if [ ! -d editor/Editor/Data/PlaybackEngines/WebGLSupport ]; then
  echo "Baixando suporte a WebGL (≈1,3 GB)..."
  curl -L --retry 5 -C - -o webgl.tar.xz "$BASE/LinuxEditorTargetInstaller/UnitySetup-WebGL-Support-for-Editor-$VERSAO.tar.xz"
  tmp=$(mktemp -d) && tar -xJf webgl.tar.xz -C "$tmp" && rm webgl.tar.xz
  mv "$(find "$tmp" -type d -name WebGLSupport | head -1)" editor/Editor/Data/PlaybackEngines/ && rm -rf "$tmp"
fi
echo "Unity instalada em $DEST/editor/Editor/Unity"
