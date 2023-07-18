using SkiaSharp;

namespace QrSharp;

public class QrCode : AbstractQrCode, IDisposable
{
    /// <summary>
    ///     Constructor without params to be used in COM Objects connections
    /// </summary>
    public QrCode()
    {
    }

    public QrCode(QrCodeData data) : base(data)
    {
    }

    public SKBitmap GetGraphic(int pixelsPerModule)
    {
        return GetGraphic(pixelsPerModule, SKColors.Black, SKColors.White, true);
    }

    public SKBitmap GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex,
        bool drawQuietZones = true)
    {
        return GetGraphic(pixelsPerModule, SKColor.Parse(darkColorHtmlHex), SKColor.Parse(lightColorHtmlHex),
            drawQuietZones);
    }


    public SKBitmap GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, bool drawQuietZones = true)
    {
        var size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
        var offset = drawQuietZones ? 0 : 4 * pixelsPerModule;

        var bmp = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        var lightPaint = new SKPaint { Color = lightColor };
        var darkPaint = new SKPaint { Color = darkColor };

        for (var x = 0; x < size + offset; x = x + pixelsPerModule)
        {
            for (var y = 0; y < size + offset; y = y + pixelsPerModule)
            {
                var module =
                    QrCodeData.ModuleMatrix[(y + pixelsPerModule) / pixelsPerModule - 1][
                        (x + pixelsPerModule) / pixelsPerModule - 1];

                canvas.DrawRect(
                    new SKRect(x - offset, y - offset, x - offset + pixelsPerModule,
                        y - offset + pixelsPerModule), module ? darkPaint : lightPaint);
            }
        }

        return bmp;
    }


    public SKBitmap GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, SKBitmap? icon = null,
        int iconSizePercent = 15, int iconBorderWidth = 0, bool drawQuietZones = true,
        SKColor? iconBackgroundColor = null)
    {
        var size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
        var offset = drawQuietZones ? 0 : 4 * pixelsPerModule;

        var bmp = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        var lightPaint = new SKPaint { Color = lightColor };
        var darkPaint = new SKPaint { Color = darkColor };
        var drawIconFlag = icon != null && iconSizePercent is > 0 and <= 100;

        canvas.Clear(lightColor);

        for (var x = 0; x < size + offset; x += pixelsPerModule)
        {
            for (var y = 0; y < size + offset; y += pixelsPerModule)
            {
                var module =
                    QrCodeData.ModuleMatrix[(y + pixelsPerModule) / pixelsPerModule - 1][
                        (x + pixelsPerModule) / pixelsPerModule - 1];
                var modulePaint = module ? darkPaint : lightPaint;

                canvas.DrawRect(
                    new SKRect(x - offset, y - offset, x - offset + pixelsPerModule, y - offset + pixelsPerModule),
                    modulePaint);
            }
        }

        if (!drawIconFlag)
        {
            return bmp;
        }

        var iconDestWidth = iconSizePercent * bmp.Width / 100f;
        var iconDestHeight = iconDestWidth * icon!.Height / icon.Width;
        var iconX = (bmp.Width - iconDestWidth) / 2;
        var iconY = (bmp.Height - iconDestHeight) / 2;

        var centerDest = new SKRect(iconX - iconBorderWidth, iconY - iconBorderWidth,
            iconX + iconDestWidth + iconBorderWidth, iconY + iconDestHeight + iconBorderWidth);
        var iconDestRect = new SKRect(iconX, iconY, iconX + iconDestWidth, iconY + iconDestHeight);

        var iconBgPaint = iconBackgroundColor != null
            ? new SKPaint { Color = iconBackgroundColor.Value }
            : lightPaint;

        if (iconBorderWidth > 0)
        {
            using var iconPath = CreateRoundedRectanglePath(centerDest, iconBorderWidth * 2);
            canvas.DrawPath(iconPath, iconBgPaint);
        }

        canvas.DrawBitmap(icon, iconDestRect);

        return bmp;
    }

    internal static SKPath CreateRoundedRectanglePath(SKRect rect, int cornerRadius)
    {
        var roundedRect = new SKPath();
        roundedRect.AddRoundRect(rect, cornerRadius, cornerRadius);
        return roundedRect;
    }
}

public static class QrCodeHelper
{
    public static SKBitmap GetQrCode(string plainText, int pixelsPerModule, SKColor darkColor, SKColor lightColor,
        QrCodeGenerator.ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false,
        QrCodeGenerator.EciMode eciMode = QrCodeGenerator.EciMode.Default,
        int requestedVersion = -1, SKBitmap? icon = null, int iconSizePercent = 15, int iconBorderWidth = 0,
        bool drawQuietZones = true)
    {
        using var qrCodeData =
            QrCodeGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
        using var qrCode = new QrCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule, darkColor, lightColor, icon, iconSizePercent, iconBorderWidth,
            drawQuietZones);
    }
}