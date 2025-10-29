#!/usr/bin/env bash
set -euo pipefail

BUILD_TARGET="StandaloneWindows64"
PROJECT_PATH="${PROJECT_PATH:-/workspace/unity/aritamR7}"
OUTPUT_PATH="${OUTPUT_PATH:-/workspace/build/StandaloneWindows64}"

mkdir -p /workspace/build
mkdir -p "${OUTPUT_PATH}"

unity-editor \
  -batchmode \
  -nographics \
  -quit \
  -logFile /workspace/build/unity.log \
  -projectPath "${PROJECT_PATH}" \
  -buildTarget "${BUILD_TARGET}" \
  -buildPlayer "${OUTPUT_PATH}/aritamR7.exe" \
  "$@"
