# cgb-unity

- シューティングゲーム「協力！ゴースト爆撃ズ」（略: CGB）の unity （フロントエンド）

## Docker での Windows 版ビルド

Unity ライセンスファイル `.Unity_lic.ulf` を使用して、Windows (x86_64) 向けビルドを Docker 上で実行できます。

### 前提条件

- Docker Desktop など Docker CLI が利用できる環境（Windows 11 で動作確認済み）
- `.Unity_lic.ulf` がリポジトリ直下に配置されていること

### ビルド手順

1. リポジトリ直下で `chmod +x scripts/build-windows.sh` を実行（初回のみ）
2. `./scripts/build-windows.sh` を実行

### 出力

- ビルド成果物は `build/StandaloneWindows64` に配置されます
- Unity ログは `build/unity.log` で確認できます

PowerShell から直接実行する場合は、以下のコマンドでも同じ結果を得られます。

```powershell
docker build -t cgb-unity-builder .
docker run --rm `
  -v ${PWD}:/workspace `
  -v ${PWD}/build:/workspace/build `
  cgb-unity-builder
```
