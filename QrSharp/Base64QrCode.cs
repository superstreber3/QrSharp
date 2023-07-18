using SkiaSharp;
using static QrSharp.QrCodeGenerator;

namespace QrSharp;

public class Base64QrCode : AbstractQrCode, IDisposable
{
    private readonly QrCode _qr;

    public Base64QrCode()
    {
        _qr = new QrCode();
    }

    public Base64QrCode(QrCodeData data) : base(data)
    {
        _qr = new QrCode(data);
    }

    public override void SetQrCodeData(QrCodeData data)
    {
        _qr.SetQrCodeData(data);
    }

    public string GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, bool drawQuietZones = true)
    {
        using var bmp = _qr.GetGraphic(pixelsPerModule, darkColor, lightColor, drawQuietZones);
        var base64 = BitmapToBase64(bmp);

        return base64;
    }

    private static string BitmapToBase64(SKBitmap bmp)
    {
        using var data = SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100);
        var base64 = Convert.ToBase64String(data.ToArray());

        return base64;
    }
}

public static class Base64QrCodeHelper
{
    public static string GetQrCode(string plainText, int pixelsPerModule, SKColor darkColor, SKColor lightColor,
        ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default,
        int requestedVersion = -1, bool drawQuietZones = true)
    {
        using var qrCodeData =
            CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
        using var qrCode = new Base64QrCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule, darkColor, lightColor, drawQuietZones);
    }
}