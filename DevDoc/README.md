# CreateQRcode 開発ドキュメント

Windows 向けの WPF アプリ。貼り付けたリンクから QR コードを作り、画面に表示し、画像ファイルまたはクリップボードへ書き出す。

文書は次の2つ。

- [仕様.md](仕様.md): 画面の動作と、生成・保存の決まり
- [ビルドと配布.md](ビルドと配布.md): 開発時の起動と、ポータブル版の作り方

## 技術

- C# / WPF
- ターゲット: `net10.0-windows`
- QR 生成: NuGet パッケージ QRCoder 1.8.1
- 名前空間: `CreateQRcode`

## ソース

| ファイル | 役割 |
| --- | --- |
| `App.xaml` | 起動時に `MainWindow.xaml` を開く |
| `MainWindow.xaml` | 一画面のレイアウト |
| `MainWindow.xaml.cs` | ボタン操作、ファイル名、保存ダイアログ |
| `QrCodeImage.cs` | QR の PNG 生成、表示用ビットマップ、ファイル保存、クリップボード |
| `CreateQRcode.csproj` | プロジェクト設定とパッケージ参照 |

画面の状態は `MainWindow` が持つ。生成済み PNG は `_pngBytes`、その元になった文字列は `_encodedText`。

## コメント

C# の関数、コンストラクタ、プロパティには、標準の XML ドキュメントコメント（`/// <summary>`、`param`、`returns`、`exception`）を付ける。コメントは日本語で、処理の内容が分かる文にする。
