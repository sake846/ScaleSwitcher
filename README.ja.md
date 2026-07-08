# ScaleSwitcher

[English](./README.md) | **日本語**

ScaleSwitcher は、WPF と .NET 10 で作成された、タスクトレイ常駐型の軽量な Windows ディスプレイ設定切り替えツールです。
複数のモニターに対して、拡大/縮小率（DPI）や画面解像度をすばやく変更できます。

![ScaleSwitcher デモ](./ScaleSwitcher.png)

## 主な機能

- **左クリックで拡大/縮小率をローテーション**: タスクトレイアイコンを左クリックするだけで、あらかじめ指定した特定の拡大率（例：100% → 150% → 100%）にすばやく切り替えます。
- **ショートカットキー（修飾キー同時押し）によるローテーション**: 左右の Shift、Control、または Alt キーを左右同時に押すことで、マウス操作不要で拡大率を瞬時に切り替えられます（設定で有効化可能）。
- **右クリックメニューの動的生成**: マルチモニターに対応し、モニターごとに解像度や拡大率のサブメニューが動的に生成されます。
- **Windows 起動時の自動起動**: スタートアップ登録（レジストリ）の有効/無効をメニューから切り替え可能です。
- **カスタマイズ可能なローテーション**: 左クリック・キー同時押しで拡大率を切り替える対象のディスプレイを選択したり、ローテーションに含める拡大/縮小率（％）をチェックボックスで簡単にカスタマイズできます。
- **ネイティブ DPI 検出**: DPI Awareness (`PerMonitorV2`) に対応しているため、現在の画面スケール設定を正確に検知・反映します。
- **多言語対応**: 日本語OS環境では自動的に日本語表記になり、それ以外の環境では英語表記になります。

## 動作環境

- **OS**: Windows 10 / 11
- **フレームワーク/ランタイム**: .NET 10.0 (WPF)

## 使い方

1. アプリケーションを起動すると、タスクトレイに常駐します。
2. 常駐アイコンを **左クリック** するか、設定された **修飾キーの左右同時押し**（例：左Shift ＋ 右Shift）をすると、指定したディスプレイの拡大/縮小率が順番に切り替わります。
3. アイコンを **右クリック** すると、詳細メニューが表示され、モニターごとの詳細な解像度や拡大率の変更、スタートアップ設定が行えます。
4. 設定ウィンドウを開くことで、ローテーション対象のディスプレイや切り替えたい拡大率の数値をチェックボックスでカスタマイズしたり、ショートカットキー用の修飾キーを変更できます。

## ビルドと実行方法

### 開発環境での実行
```bash
dotnet run
```

### プロジェクトのビルド
```bash
dotnet build
```

### リリースビルドの作成
```bash
dotnet build -c Release
```
ビルドされた実行ファイル（`.exe`）は、以下のディレクトリに出力されます。
`bin/Release/net10.0-windows/ScaleSwitcher.exe`

## 設定と構成

### 設定ファイル
ユーザーの設定情報は JSON 形式で以下のパスに保存されます。

- **保存先**: `%LOCALAPPDATA%\ScaleSwitcher\settings.json`

#### 設定ファイルの例
```json
{
  "TargetMonitorIndex": 0,
  "ActiveDpiPercentages": [
    100,
    200
  ],
  "DisplayNumberSource": "TargetId",
  "UseCustomDisplayName": false,
  "CustomDisplayName": "",
  "KeyboardSwitchMode": "Shift",
  "UiLanguage": "auto"
}
```

#### 各設定項目の説明
- `TargetMonitorIndex`: ローテーション対象となるディスプレイのインデックス（0始まり）。
- `ActiveDpiPercentages`: ローテーションに含める DPI パーセンテージ数値のリスト。
- `DisplayNumberSource`: ディスプレイ番号を割り当てるためのロジック。
  - `TargetId` (規定値): APIから取得したターゲットIDに基づく番号。
  - `PathOrder`: ディスプレイの配置パス順。
  - `GdiDeviceName`: GDIデバイス名に基づく番号。
- `UseCustomDisplayName`: ディスプレイ表示にカスタム名を使用するかどうか。
- `CustomDisplayName`: カスタム名として使用する文字列。
- `KeyboardSwitchMode`: 左右同時押しでローテーションをトリガーするキーの種類。
  - `Shift` (規定値): 左右の `Shift` キーの同時押しでトリガー。
  - `Control`: 左右の `Ctrl` キーの同時押しでトリガー。
  - `Alt`: 左右の `Alt` キーの同時押しでトリガー。
  - `Off`: キーによる切り替えを無効化。
- `UiLanguage`: 表示言語設定。`"auto"` のほか、`"ja"`, `"en"` が指定可能です。

## 技術情報 / 仕様

- **技術スタック**: C# / WPF (.NET 10)
- **API制御**: Win32 APIによる制御 (`user32.dll`, `shcore.dll` の P/Invoke)
- **DPI制御**: ネイティブの Windows DPI Awareness 設定 (`app.manifest` を使用)
- **タスクトレイ管理**: Windows Forms の `NotifyIcon` をラップして使用（サードパーティライブラリ不使用）

## ライセンス

本プロジェクトは [MIT ライセンス](./LICENSE) のもとで公開されています。
