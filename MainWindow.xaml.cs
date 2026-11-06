using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using QRCoder.Exceptions;

namespace CreateQRcode;

public partial class MainWindow : Window
{
    private byte[]? _pngBytes;
    private string? _encodedText;

    /// <summary>
    /// メイン画面を初期化する。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// URL 欄の内容から QR コードを作り、プレビューに表示する。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">イベントデータ。</param>
    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var text = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = "URLを入力してください。";
            UrlTextBox.Focus();
            return;
        }

        if (!IsAbsoluteUrl(text))
        {
            MessageBox.Show(
                this,
                "URL以外は変換できません。",
                "警告",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            UrlTextBox.Focus();
            return;
        }

        try
        {
            var png = QrCodeImage.CreatePng(text);
            ShowQrCode(text, png);
            StatusText.Text = "QRコードを作成しました。コピーまたは保存ができます。";
        }
        catch (DataTooLongException)
        {
            ClearPreview();
            StatusText.Text = "文字数が多すぎてQRコードにできません。";
        }
        catch (Exception)
        {
            ClearPreview();
            StatusText.Text = "QRコードを作成できませんでした。";
        }
    }

    /// <summary>
    /// 表示中の QR コードを、選んだ場所と形式で保存する。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">イベントデータ。</param>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pngBytes is null || _encodedText is null)
        {
            StatusText.Text = "先に作成ボタンでQRコードを表示してください。";
            return;
        }

        if (!string.Equals(_encodedText, UrlTextBox.Text.Trim(), StringComparison.Ordinal))
        {
            StatusText.Text = "URLが変わっています。もう一度作成してください。";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "QRコードを保存",
            Filter = "PNG 画像 (*.png)|*.png|JPEG 画像 (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP 画像 (*.bmp)|*.bmp",
            FilterIndex = 1,
            DefaultExt = ".png",
            AddExtension = true,
            FileName = SuggestFileName(_encodedText),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var path = ApplySelectedFormat(dialog.FileName, dialog.FilterIndex);
        try
        {
            QrCodeImage.Save(_pngBytes, path);
            StatusText.Text = "保存しました: " + path;
        }
        catch (Exception ex)
        {
            StatusText.Text = "保存に失敗しました。";
            MessageBox.Show(
                this,
                "ファイルを保存できませんでした。" + Environment.NewLine + ex.Message,
                "保存エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 表示中の QR コードをクリップボードへコピーする。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">イベントデータ。</param>
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pngBytes is null || _encodedText is null)
        {
            StatusText.Text = "先に作成ボタンでQRコードを表示してください。";
            return;
        }

        if (!string.Equals(_encodedText, UrlTextBox.Text.Trim(), StringComparison.Ordinal))
        {
            StatusText.Text = "URLが変わっています。もう一度作成してください。";
            return;
        }

        try
        {
            QrCodeImage.CopyToClipboard(_pngBytes);
            StatusText.Text = "クリップボードにコピーしました。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "クリップボードへのコピーに失敗しました。";
            MessageBox.Show(
                this,
                "クリップボードにコピーできませんでした。" + Environment.NewLine + ex.Message,
                "コピーエラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// URL 欄を空にし、入力待ちの表示に戻す。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">イベントデータ。</param>
    private void ClearUrlButton_Click(object sender, RoutedEventArgs e)
    {
        UrlTextBox.Clear();
        UrlTextBox.Focus();
        StatusText.Text = "URLを貼り付けて、作成を押してください。";
    }

    /// <summary>
    /// URL の有無で ✕ の表示を切り替え、作成後に内容が変わったらプレビューを消す。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">変更内容。</param>
    private void UrlTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ClearUrlButton.Visibility = string.IsNullOrEmpty(UrlTextBox.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (_encodedText is null)
        {
            return;
        }

        if (string.Equals(_encodedText, UrlTextBox.Text.Trim(), StringComparison.Ordinal))
        {
            return;
        }

        ClearPreview();
        StatusText.Text = "URLが変わったので、もう一度作成してください。";
    }

    /// <summary>
    /// URL 欄で Enter が押されたとき、QR コードを作成する。
    /// </summary>
    /// <param name="sender">イベントの送信元。</param>
    /// <param name="e">押されたキー。</param>
    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CreateButton_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    /// <summary>
    /// 作成した QR コードをプレビューし、コピーと保存を可能にする。
    /// </summary>
    /// <param name="text">符号化した文字列。</param>
    /// <param name="png">表示する PNG。</param>
    private void ShowQrCode(string text, byte[] png)
    {
        _encodedText = text;
        _pngBytes = png;
        QrImage.Source = QrCodeImage.ToBitmap(png);
        PlaceholderText.Visibility = Visibility.Collapsed;
        CopyButton.IsEnabled = true;
        SaveButton.IsEnabled = true;
    }

    /// <summary>
    /// プレビューを消し、コピーと保存を押せなくする。
    /// </summary>
    private void ClearPreview()
    {
        _encodedText = null;
        _pngBytes = null;
        QrImage.Source = null;
        PlaceholderText.Visibility = Visibility.Visible;
        CopyButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
    }

    /// <summary>
    /// スキーム付きの絶対 URL かどうかを判定する。文字列は書き換えない。
    /// </summary>
    /// <param name="text">判定する文字列。</param>
    /// <returns>絶対 URL なら true。</returns>
    private static bool IsAbsoluteUrl(string text)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out _);
    }

    /// <summary>
    /// 保存ダイアログの初期ファイル名を、日時の接頭辞付きで作る。
    /// </summary>
    /// <param name="text">QR コードにした文字列。</param>
    /// <returns><c>yyyyMMddHHmm_qrcode_ホスト名.png</c>。ホストが取れないときは <c>yyyyMMddHHmm_qrcode.png</c>。</returns>
    private static string SuggestFileName(string text)
    {
        var prefix = DateTime.Now.ToString("yyyyMMddHHmm");
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            var invalid = Path.GetInvalidFileNameChars();
            var host = string.Concat(uri.Host.Select(ch => invalid.Contains(ch) ? '_' : ch));
            return prefix + "_qrcode_" + host + ".png";
        }

        return prefix + "_qrcode.png";
    }

    /// <summary>
    /// 保存ダイアログで選んだ形式に、ファイル名の拡張子を合わせる。
    /// </summary>
    /// <param name="fileName">ダイアログが返したファイル名。</param>
    /// <param name="filterIndex">選択中のフィルタ。1 が PNG、2 が JPEG、3 が BMP。</param>
    /// <returns>選んだ形式の拡張子を持つファイル名。JPEG で <c>.jpeg</c> と指定された場合はそのまま。</returns>
    private static string ApplySelectedFormat(string fileName, int filterIndex)
    {
        var selected = filterIndex switch
        {
            2 => ".jpg",
            3 => ".bmp",
            _ => ".png"
        };

        var current = Path.GetExtension(fileName);
        var matches = selected == ".jpg"
            ? current.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || current.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            : current.Equals(selected, StringComparison.OrdinalIgnoreCase);

        return matches ? fileName : Path.ChangeExtension(fileName, selected);
    }
}
