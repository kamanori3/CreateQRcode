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

    public MainWindow()
    {
        InitializeComponent();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var text = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = "URLを入力してください。";
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

    private void ClearUrlButton_Click(object sender, RoutedEventArgs e)
    {
        UrlTextBox.Clear();
        UrlTextBox.Focus();
        StatusText.Text = "URLを貼り付けて、作成を押してください。";
    }

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

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CreateButton_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void ShowQrCode(string text, byte[] png)
    {
        _encodedText = text;
        _pngBytes = png;
        QrImage.Source = QrCodeImage.ToBitmap(png);
        PlaceholderText.Visibility = Visibility.Collapsed;
        CopyButton.IsEnabled = true;
        SaveButton.IsEnabled = true;
    }

    private void ClearPreview()
    {
        _encodedText = null;
        _pngBytes = null;
        QrImage.Source = null;
        PlaceholderText.Visibility = Visibility.Visible;
        CopyButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
    }

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
