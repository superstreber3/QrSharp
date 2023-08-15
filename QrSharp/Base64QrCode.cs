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

	public string GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, SKBitmap? icon = null,
	  int iconSizePercent = 15, int iconBorderWidth = 0, bool drawQuietZones = true,
	  SKColor? iconBackgroundColor = null)
	{
		var size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
		var offset = drawQuietZones ? 0 : 4 * pixelsPerModule;
		var base64 = string.Empty;
		var bmp = new SKBitmap(size, size);
		using var canvas = new SKCanvas(bmp);
		var lightPaint = new SKPaint { Color = lightColor };
		var darkPaint = new SKPaint { Color = darkColor };
		var drawIconFlag = icon is not null && iconSizePercent is > 0 and <= 100;

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
			base64 = BitmapToBase64(bmp);
			return base64;
		}

		var iconDestWidth = iconSizePercent * bmp.Width / 100f;
		var iconDestHeight = iconDestWidth * icon!.Height / icon.Width;
		var iconX = (bmp.Width - iconDestWidth) / 2;
		var iconY = (bmp.Height - iconDestHeight) / 2;

		var centerDest = new SKRect(iconX - iconBorderWidth, iconY - iconBorderWidth,
			iconX + iconDestWidth + iconBorderWidth, iconY + iconDestHeight + iconBorderWidth);
		var iconDestRect = new SKRect(iconX, iconY, iconX + iconDestWidth, iconY + iconDestHeight);

		var iconBgPaint = iconBackgroundColor is not null
			? new SKPaint { Color = iconBackgroundColor.Value }
			: lightPaint;

		if (iconBorderWidth > 0)
		{
			using var iconPath = CreateRoundedRectanglePath(centerDest, iconBorderWidth * 2);
			canvas.DrawPath(iconPath, iconBgPaint);
		}

		canvas.DrawBitmap(icon, iconDestRect);
		base64 = BitmapToBase64(bmp);

		return base64;
	}

	internal static SKPath CreateRoundedRectanglePath(SKRect rect, int cornerRadius)
	{
		var roundedRect = new SKPath();
		roundedRect.AddRoundRect(rect, cornerRadius, cornerRadius);
		return roundedRect;
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