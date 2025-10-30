# cgb-unity

- シューティングゲーム「協力！ゴースト爆撃ズ」（略: CGB）の unity （フロントエンド）
- C# 8
- unity 6000.0.41f1

## Hub 連携

- `cgb-io-hub` を `ws://<Hubのホスト>:8765/ws` で起動しておくと、ゲーム起動時に `role: game` として自動登録されます。
- 接続先は環境変数 `CGB_HUB_URL` で上書きできます。
  - 例: `CGB_HUB_URL=ws://192.168.0.10:8765/ws ./unity/aritamR7/aritamR7.exe`
- Hub に接続済みの Web コントローラから届く `p1`〜`p4` の入力がゲーム内ターゲットへ反映されます。

## Docker での Windows 版ビルド

Unity ライセンスファイル `.Unity_lic.ulf` を使用して、
Windows (x86_64) 向けビルドを Docker 上で実行できる。
デフォルトの Dockerfile は `unityci/editor:ubuntu-6000.0.41f1-windows-mono-3.2.0`
を利用し、Mono バックエンドでの Windows ビルドに対応している。

# Useage

## ライセンス申請ファイル作成

- 次を実行し、`build/Unity_v6000.0.41f1.alf` を生成する
- ログは `build/` に残る

```bash
mkdir -p build/
docker run --rm \
  -v "${PWD}/build:/root" \
  unityci/editor:ubuntu-6000.0.41f1-windows-mono-3.2.0 \
  bash -lc '
    unity-editor \
      -batchmode -nographics -quit \
      -createManualActivationFile \
      -logFile /root/unity-manual.log && \
    cp /Unity_v6000.0.41f1.alf /root/Unity_v6000.0.41f1.alf'
```

## Unity ID でライセンス発行

- 生成された `.alf` または `.xml` を Unity にアップロード
  (https://license.unity3d.com/manual) し、CI 用マシン向けの .ulf を取得
  - 関連: https://github.com/game-ci/documentation/issues/408
  - 関連: https://zenn.dev/hirosukekayaba/articles/067693ad146d18

## ライセンスファイル差し替え

新たな `.Unity_lic.ulf` をリポジトリ直下に配置し直したうえで、
`./scripts/build-windows.sh` を再実行

## ビルド

1. リポジトリ直下で `chmod +x scripts/build-windows.sh` を実行（初回のみ）
2. `./scripts/build-windows.sh` を実行

## 出力

- ビルド成果物は `build/StandaloneWindows64` に配置されます
- Unity ログは `build/unity.log` で確認できます

PowerShell から直接実行する場合は、以下のコマンドでも同じ結果を得られる

```powershell
docker build -t cgb-unity-builder .
docker run --rm `
  -v ${PWD}:/workspace `
  -v ${PWD}/build:/workspace/build `
  cgb-unity-builder
```

## 補足: 別の UnityCI イメージを利用したい場合

- `docker build` 時に `--build-arg UNITYCI_IMAGE=unityci/editor:<tag>` を指定
- `UNITYCI_IMAGE=unityci/editor:<tag> ./scripts/build-windows.sh` のように環境変数を渡す
