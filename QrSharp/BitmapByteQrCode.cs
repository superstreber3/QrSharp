using System.Globalization;
using static QrSharp.QrCodeGenerator;

namespace QrSharp;

public class BitmapByteQrCode : AbstractQrCode, IDisposable
{
    /// <summary>
    ///     Constructor without params to be used in COM Objects connections
    /// </summary>
    public BitmapByteQrCode()
    {
    }

    public BitmapByteQrCode(QrCodeData data) : base(data)
    {
    }

    public byte[] GetGraphic(int pixelsPerModule)
    {
        return GetGraphic(pixelsPerModule, new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0xFF, 0xFF, 0xFF });
    }

    public byte[] GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex)
    {
        return GetGraphic(pixelsPerModule, HexColorToByteArray(darkColorHtmlHex),
            HexColorToByteArray(lightColorHtmlHex));
    }

    public byte[] GetGraphic(int pixelsPerModule, byte[] darkColorRgb, byte[] lightColorRgb)
    {
        var sideLength = QrCodeData.ModuleMatrix.Count * pixelsPerModule;

        var moduleDark = darkColorRgb.Reverse().ToArray();
        var moduleLight = lightColorRgb.Reverse().ToArray();

        var bmp = new List<byte>();

        //header
        bmp.AddRange(new byte[]
        {
            0x42, 0x4D, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00
        });

        //width
        bmp.AddRange(IntTo4Byte(sideLength));
        //height
        bmp.AddRange(IntTo4Byte(sideLength));

        //header end
        bmp.AddRange(new byte[] { 0x01, 0x00, 0x18, 0x00 });

        //draw Qr code
        for (var x = sideLength - 1; x >= 0; x -= pixelsPerModule)
        {
            for (var pm = 0; pm < pixelsPerModule; pm++)
            {
                for (var y = 0; y < sideLength; y += pixelsPerModule)
                {
                    var module =
                        QrCodeData.ModuleMatrix[(x + pixelsPerModule) / pixelsPerModule - 1][
                            (y + pixelsPerModule) / pixelsPerModule - 1];
                    for (var i = 0; i < pixelsPerModule; i++)
                    {
                        bmp.AddRange(module ? moduleDark : moduleLight);
                    }
                }

                if (sideLength % 4 == 0)
                {
                    continue;
                }

                {
                    for (var i = 0; i < sideLength % 4; i++)
                    {
                        bmp.Add(0x00);
                    }
                }
            }
        }

        //finalize with terminator
        bmp.AddRange(new byte[] { 0x00, 0x00 });

        return bmp.ToArray();
    }

    private static byte[] HexColorToByteArray(string colorString)
    {
        if (colorString.StartsWith("#"))
        {
            colorString = colorString[1..];
        }

        var byteColor = new byte[colorString.Length / 2];
        for (var i = 0; i < byteColor.Length; i++)
        {
            byteColor[i] = byte.Parse(colorString.AsSpan(i * 2, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);
        }

        return byteColor;
    }

    private static IEnumerable<byte> IntTo4Byte(int inp)
    {
        var bytes = new byte[2];
        unchecked
        {
            bytes[1] = (byte)(inp >> 8);
            bytes[0] = (byte)inp;
        }

        return bytes;
    }
}

public static class BitmapByteQrCodeHelper
{
    public static byte[] GetQrCode(string plainText, int pixelsPerModule, string darkColorHtmlHex,
        string lightColorHtmlHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false,
        EciMode eciMode = EciMode.Default, int requestedVersion = -1)
    {
        using var qrCodeData = CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode,
            requestedVersion);
        using var qrCode = new BitmapByteQrCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule, darkColorHtmlHex, lightColorHtmlHex);
    }

    public static byte[] GetQrCode(string txt, ECCLevel eccLevel, int size)
    {
        using var qrCode = CreateQrCode(txt, eccLevel);
        using var qrBmp = new BitmapByteQrCode(qrCode);
        return qrBmp.GetGraphic(size);
    }
}