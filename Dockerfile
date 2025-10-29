ARG UNITYCI_IMAGE=unityci/editor:ubuntu-6000.0.41f1-base-3.2.0
FROM ${UNITYCI_IMAGE}

RUN apt-get update \
  && apt-get install -y --no-install-recommends python3 python3-pip \
  && pip3 install --no-cache-dir unity-downloader-cli \
  && rm -rf /var/lib/apt/lists/*

ARG UNITY_VERSION=6000.0.41f1

RUN unity-downloader-cli \
      --module Windows-IL2CPP \
      --module Windows-Mono \
      --fast \
      --unity-version "${UNITY_VERSION}" \
  && rm -rf /root/.cache/unity3d

ENV UNITY_LICENSE_FILE=/root/.local/share/unity3d/Unity/Unity_lic.ulf \
    PROJECT_PATH=/workspace/unity/aritamR7 \
    OUTPUT_PATH=/workspace/build/StandaloneWindows64

RUN mkdir -p $(dirname "$UNITY_LICENSE_FILE")

COPY .Unity_lic.ulf ${UNITY_LICENSE_FILE}

RUN chmod 600 "${UNITY_LICENSE_FILE}"

COPY docker/entrypoint.sh /usr/local/bin/unity-build

RUN chmod +x /usr/local/bin/unity-build

WORKDIR /workspace

ENTRYPOINT ["/usr/local/bin/unity-build"]
