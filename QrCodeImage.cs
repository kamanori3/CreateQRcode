using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;

namespace CreateQRcode;

internal static class QrCodeImage
{
    /// <summary>
    /// 文字列を QR コードの PNG バイト列にする。
    /// </summary>
    /// <param name="text">符号化する文字列。</param>
    /// <returns>余白付きの白黒 PNG。</returns>
    /// <exception cref="QRCoder.Exceptions.DataTooLongException">文字列が QR コードの容量を超えたとき。</exception>
    public static byte[] CreatePng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            text,
            QRCodeGenerator.ECCLevel.Q,
            forceUtf8: true,
            utf8BOM: false,
            eciMode: QRCodeGenerator.EciMode.Utf8,
            requestedVersion: -1);

        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pixelsPerModule: 20, drawQuietZones: true);
    }

    /// <summary>
    /// PNG バイト列を画面表示用のビットマップにする。
    /// </summary>
    /// <param name="pngBytes">表示する PNG。</param>
    /// <returns>ストリームを閉じたあとも使える、固定済みのビットマップ。</returns>
    public static BitmapSource ToBitmap(byte[] pngBytes)
    {
        using var stream = new MemoryStream(pngBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>
    /// PNG バイト列を指定パスへ保存する。拡張子が PNG 以外なら JPEG または BMP に変換する。
    /// </summary>
    /// <param name="pngBytes">保存する PNG。</param>
    /// <param name="path">拡張子を含む保存先パス。</param>
    /// <exception cref="InvalidOperationException">拡張子が PNG、JPEG、BMP のいずれでもないとき。</exception>
    public static void Save(byte[] pngBytes, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".png")
        {
            File.WriteAllBytes(path, pngBytes);
            return;
        }

        var source = ToBitmap(pngBytes);
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgr24, null, 0);
        converted.Freeze();

        BitmapEncoder encoder = extension switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 100 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => throw new InvalidOperationException("対応していないファイル形式です。PNG、JPEG、BMP から選んでください。")
        };

        encoder.Frames.Add(BitmapFrame.Create(converted));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    /// <summary>
    /// QR コードの画像をクリップボードへ置く。ビットマップと元の PNG の両方を載せる。
    /// </summary>
    /// <param name="pngBytes">コピーする PNG。</param>
    /// <exception cref="ExternalException">クリップボードを開けなかったとき。</exception>
    public static void CopyToClipboard(byte[] pngBytes)
    {
        var source = ToBitmap(pngBytes);
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        BitmapSource bitmap = converted;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var pngStream = new MemoryStream(pngBytes);
                var data = new DataObject();
                data.SetImage(bitmap);
                data.SetData("PNG", pngStream);
                Clipboard.SetDataObject(data, true);
                return;
            }
            catch (ExternalException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
