# GitHub Actions自動ビルド

このリポジトリでは、GitHub ActionsとGameCIを使用して、XRデバイス向けのビルドを自動化しています。

## 対応デバイス

現在、以下の2つのSDK向けの自動ビルドに対応しています：

1. **Meta OpenXR SDK** (Meta Quest対応)
2. **PICO Unity OpenXR SDK** (PICO 4など対応)

## ビルドのトリガー方法

### 自動ビルド
- `develop`ブランチにマージ（またはプッシュ）されたタイミングで自動的にビルドが実行されます

### 手動ビルド
1. GitHubリポジトリページの「Actions」タブに移動
2. 左側のワークフロー一覧から実行したいビルドを選択：
   - "Build Meta OpenXR (Quest)"
   - "Build PICO Unity OpenXR"
3. 右上の「Run workflow」ボタンをクリック
4. ブランチを選択して「Run workflow」をクリック

## 必要なシークレット設定

GitHub Actionsを実行するには、以下のシークレットをリポジトリ設定で追加する必要があります：

- `UNITY_LICENSE`: Unity Proライセンス（GameCI用）
- `UNITY_EMAIL`: Unityアカウントのメールアドレス
- `UNITY_PASSWORD`: Unityアカウントのパスワード

### シークレットの設定方法
1. リポジトリの「Settings」→「Secrets and variables」→「Actions」に移動
2. 「New repository secret」をクリック
3. 上記の各シークレットを追加

## ビルド成果物

ビルドが成功すると、以下の場所にAPKファイルがアーティファクトとして保存されます：

- Meta OpenXR: `MetaOpenXRBuild` (build/Android/MetaOpenXR.apk)
- PICO Unity OpenXR: `PicoOpenXRBuild` (build/Android/PicoOpenXR.apk)

アーティファクトは、GitHub Actionsのワークフローページからダウンロードできます。

## ワークフローファイル

- `.github/workflows/build-meta-openxr.yml`: Meta OpenXR SDK用
- `.github/workflows/build-pico-openxr.yml`: PICO Unity OpenXR SDK用

## ビルドスクリプト

各SDK向けのビルドスクリプトは以下にあります：

- `Packages/com.styly.styly-xr-rig/Editor/Build/BuildForMetaOpenXrSdk.cs`
- `Packages/com.styly.styly-xr-rig/Editor/Build/BuildForPicoUnityOpenXrSdk.cs`

これらのスクリプトは、SDK設定とビルドを一括で行うためにGameCIから呼び出されます。

## ビルドプロセス

1. リポジトリのチェックアウト（Git LFS対応）
2. ディスク容量の確保
3. Unity Libraryのキャッシュ
4. Unity Projectのビルド（SDK設定を含む）
5. ビルド成果物のアップロード

## トラブルシューティング

### ビルドが失敗する場合

1. Unity Licenseが正しく設定されているか確認
2. 必要なシーンがBuild Settingsに含まれているか確認
3. ワークフローログで詳細なエラーメッセージを確認

### Unity Licenseの取得方法

GameCIの公式ドキュメントを参照してください：
https://game.ci/docs/github/activation

## 参考資料

- [GameCI公式ドキュメント](https://game.ci/)
- [PICO4-MR-Unity-Template（参考リポジトリ）](https://github.com/afjk/PICO4-MR-Unity-Template)
