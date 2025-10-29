#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="${PROJECT_PATH:-/workspace/unity/aritamR7}"
OUTPUT_PATH="${OUTPUT_PATH:-/workspace/build/StandaloneWindows64}"
OUTPUT_FILE="${OUTPUT_PATH}/aritamR7.exe"

mkdir -p /workspace/build
mkdir -p "${OUTPUT_PATH}"

unity-editor \
  -batchmode \
  -nographics \
  -quit \
  -logFile /workspace/build/unity.log \
  -projectPath "${PROJECT_PATH}" \
  -buildWindows64Player "${OUTPUT_FILE}" \
  "$@"
