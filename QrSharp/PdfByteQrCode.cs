using System.Globalization;
using System.Text;
using SkiaSharp;
using static QrSharp.QrCodeGenerator;

namespace QrSharp;

public class PdfByteQrCode : AbstractQrCode, IDisposable
{
    private readonly byte[] _pdfBinaryComment = { 0x25, 0xe2, 0xe3, 0xcf, 0xd3 };

    /// <summary>
    ///     Constructor without params to be used in COM Objects connections
    /// </summary>
    public PdfByteQrCode()
    {
    }

    public PdfByteQrCode(QrCodeData data) : base(data)
    {
    }

    /// <summary>
    ///     Takes hexadecimal color string #000000 and returns byte[]{ 0, 0, 0 }
    /// </summary>
    /// <param name="colorString">Color in HEX format like #ffffff</param>
    /// <returns></returns>
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

    private static SKColor HexColorToSkColor(string colorString)
    {
        var color = new SKColor();

        if (!string.IsNullOrEmpty(colorString) && colorString[0] == '#')
        {
            color = SKColor.Parse(colorString);
        }

        return color;
    }

    /// <summary>
    ///     Creates a PDF document with given colors DPI and quality
    /// </summary>
    /// <param name="pixelsPerModule"></param>
    /// <param name="darkColorHtmlHex"></param>
    /// <param name="lightColorHtmlHex"></param>
    /// <param name="dpi"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    public byte[] GetGraphic(int pixelsPerModule, string darkColorHtmlHex = "#000000",
        string lightColorHtmlHex = "#ffffff", int dpi = 150,
        long quality = 85)
    {
        var jpgArray = Array.Empty<byte>();
        var darkColor = HexColorToSkColor(darkColorHtmlHex);
        var lightColor = HexColorToSkColor(lightColorHtmlHex);

        var imgSize = QrCodeData.ModuleMatrix.Count * pixelsPerModule;
        var pdfMediaSize = (imgSize * 72 / dpi).ToString(CultureInfo.InvariantCulture);

        var info = new SKImageInfo(imgSize, imgSize, SKColorType.Rgba8888);

        using (var surface = SKSurface.Create(info))
        {
            var canvas = surface.Canvas;

            // draw QR code
            for (var x = 0; x < QrCodeData.ModuleMatrix.Count; x++)
            {
                for (var y = 0; y < QrCodeData.ModuleMatrix.Count; y++)
                {
                    var module = QrCodeData.ModuleMatrix[x][y];

                    var rect = new SKRect(x * pixelsPerModule, y * pixelsPerModule,
                        x * pixelsPerModule + pixelsPerModule, y * pixelsPerModule + pixelsPerModule);

                    canvas.DrawRect(rect,
                        module
                            ? new SKPaint { Color = darkColor, IsAntialias = false }
                            : new SKPaint { Color = lightColor, IsAntialias = false });
                }
            }

            using (var image = surface.Snapshot())
            using (var data = image.Encode(SKEncodedImageFormat.Jpeg, (int)quality))
            {
                data.ToArray();
            }
        }

        //Create PDF document
        using (var stream = new MemoryStream())
        {
            var writer = new StreamWriter(stream, Encoding.GetEncoding("ASCII"));

            var xrefs = new List<long>();

            writer.Write("%PDF-1.5\r\n");
            writer.Flush();

            stream.Write(_pdfBinaryComment, 0, _pdfBinaryComment.Length);
            writer.WriteLine();

            writer.Flush();
            xrefs.Add(stream.Position);

            writer.Write(
                xrefs.Count + " 0 obj\r\n" +
                "<<\r\n" +
                "/Type /Catalog\r\n" +
                "/Pages 2 0 R\r\n" +
                ">>\r\n" +
                "endobj\r\n"
            );

            writer.Flush();
            xrefs.Add(stream.Position);

            writer.Write(
                xrefs.Count + " 0 obj\r\n" +
                "<<\r\n" +
                "/Count 1\r\n" +
                "/Kids [ <<\r\n" +
                "/Type /Page\r\n" +
                "/Parent 2 0 R\r\n" +
                "/MediaBox [0 0 " + pdfMediaSize + " " + pdfMediaSize + "]\r\n" +
                "/Resources << /ProcSet [ /PDF /ImageC ]\r\n" +
                "/XObject << /Im1 4 0 R >> >>\r\n" +
                "/Contents 3 0 R\r\n" +
                ">> ]\r\n" +
                ">>\r\n" +
                "endobj\r\n"
            );

            var x = "q\r\n" +
                    pdfMediaSize + " 0 0 " + pdfMediaSize + " 0 0 cm\r\n" +
                    "/Im1 Do\r\n" +
                    "Q";

            writer.Flush();
            xrefs.Add(stream.Position);

            writer.Write(
                xrefs.Count + " 0 obj\r\n" +
                "<< /Length " + x.Length + " >>\r\n" +
                "stream\r\n" +
                x + "endstream\r\n" +
                "endobj\r\n"
            );

            writer.Flush();
            xrefs.Add(stream.Position);

            writer.Write(
                xrefs.Count + " 0 obj\r\n" +
                "<<\r\n" +
                "/Name /Im1\r\n" +
                "/Type /XObject\r\n" +
                "/Subtype /Image\r\n" +
                "/Width " + imgSize + "/Height " + imgSize + "/Length 5 0 R\r\n" +
                "/Filter /DCTDecode\r\n" +
                "/ColorSpace /DeviceRGB\r\n" +
                "/BitsPerComponent 8\r\n" +
                ">>\r\n" +
                "stream\r\n"
            );
            writer.Flush();
            stream.Write(jpgArray, 0, jpgArray.Length);
            writer.Write(
                "\r\n" +
                "endstream\r\n" +
                "endobj\r\n"
            );

            writer.Flush();
            xrefs.Add(stream.Position);

            writer.Write(
                xrefs.Count + " 0 obj\r\n" +
                jpgArray.Length + " endobj\r\n"
            );

            writer.Flush();
            var startxref = stream.Position;

            writer.Write(
                "xref\r\n" +
                "0 " + (xrefs.Count + 1) + "\r\n" +
                "0000000000 65535 f\r\n"
            );

            foreach (var refValue in xrefs)
            {
                writer.Write(refValue.ToString("0000000000") + " 00000 n\r\n");
            }

            writer.Write(
                "trailer\r\n" +
                "<<\r\n" +
                "/Size " + (xrefs.Count + 1) + "\r\n" +
                "/Root 1 0 R\r\n" +
                ">>\r\n" +
                "startxref\r\n" +
                startxref + "\r\n" +
                "%%EOF"
            );

            writer.Flush();

            stream.Position = 0;

            return stream.ToArray();
        }
    }
}

public static class PdfByteQrCodeHelper
{
    public static byte[] GetQrCode(string plainText, int pixelsPerModule, string darkColorHtmlHex,
        string lightColorHtmlHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false,
        EciMode eciMode = EciMode.Default, int requestedVersion = -1)
    {
        using var qrCodeData = CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode,
            requestedVersion);
        using var qrCode = new PdfByteQrCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule, darkColorHtmlHex, lightColorHtmlHex);
    }

    public static byte[] GetQrCode(string txt, ECCLevel eccLevel, int size)
    {
        using var qrCode = CreateQrCode(txt, eccLevel);
        using var qrBmp = new PdfByteQrCode(qrCode);
        return qrBmp.GetGraphic(size);
    }
}