#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
IMAGE_NAME="cgb-unity-builder"
BUILD_CACHE_DIR="${REPO_ROOT}/build"
DOCKER_CONFIG_DIR="${REPO_ROOT}/.docker-config"

BUILD_ARGS=()
if [[ -n "${UNITYCI_IMAGE:-}" ]]; then
  BUILD_ARGS+=("--build-arg" "UNITYCI_IMAGE=${UNITYCI_IMAGE}")
fi

mkdir -p "${BUILD_CACHE_DIR}"
mkdir -p "${DOCKER_CONFIG_DIR}"

export DOCKER_CONFIG="${DOCKER_CONFIG_DIR}"

docker build -t "${IMAGE_NAME}" "${BUILD_ARGS[@]}" "${REPO_ROOT}"

docker run --rm \
  -v "${REPO_ROOT}:/workspace" \
  -v "${BUILD_CACHE_DIR}:/workspace/build" \
  "${IMAGE_NAME}" "$@"
