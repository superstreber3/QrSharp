using QrSharp;
using QrSharp.RenderTypes;
using QrSharpTest.Helpers;
using Shouldly;
using SkiaSharp;

namespace QrSharpTest;

/****************************************************************************************************
 * Note: Test cases compare the outcome visually even if it's slower than a byte-wise compare.
 *       This is necessary, because the Deflate implementation differs on the different target
 *       platforms and thus the outcome, even if visually identical, differs. Thus only a visual
 *       test method makes sense. In addition bytewise differences shouldn't be important, if the
 *       visual outcome is identical and thus the qr code is identical/scannable.
 *
 *       The Hashes from QrSharp differ from the hashes from the original QrCoder library. This is
 *       because SkiaSharp and System.Drawing differ in their implementation of the deflate
 *       method. The outcome is visually identical, but the bytes differ.
 ****************************************************************************************************/
public class PngByteQrCodeRendererTests
{
    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_blackwhite()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        var pngCodeGfx = new PngByteQrCode(data).GetGraphic(5);
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("26b8ed4409b4e236c75f19eb84c4159b");
    }

    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_color()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        var pngCodeGfx = new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 0, 0 }, new byte[] { 0, 0, 255 });
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("5cd8d5d79a72ebbb33f210b134773bc0");
    }


    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_color_with_alpha()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        var pngCodeGfx =
            new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 });
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("8b76d92113728b5a5b01e4007ebc004a");
    }

    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_color_without_quietzones()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        var pngCodeGfx =
            new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 }, false);
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("d3c9ba9fba46dd979ddf6a04af8ef995");
    }

    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_instantate_pngbyte_qrcode_parameterless()
    {
        var pngCode = new PngByteQrCode();
        pngCode.ShouldNotBeNull();
        pngCode.ShouldBeOfType<PngByteQrCode>();
    }

    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_from_helper()
    {
        //Create QR code                   
        var pngCodeGfx = PngByteQrCodeHelper.GetQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L, 10);
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("bb064fb17c691502d3ebcf9995a5e008");
    }

    [Fact]
    [Category("QRRenderer/PngByteQrCode")]
    public void can_render_pngbyte_qrcode_from_helper_2()
    {
        //Create QR code                   
        var pngCodeGfx = PngByteQrCodeHelper.GetQrCode("This is a quick test! 123#?", 5,
            new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 }, QrCodeGenerator.ECCLevel.L);
        var bmp = SKBitmap.Decode(pngCodeGfx);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("8b76d92113728b5a5b01e4007ebc004a");
    }
}