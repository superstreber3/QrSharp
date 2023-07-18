using QrSharp;
using QrSharpTest.Helpers;
using Shouldly;
using SkiaSharp;

namespace QrSharpTest;

public class QrCodeRendererTest
{
    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_standard_graphic()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var bmp = new QrCode(data).GetGraphic(10);

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("1e0afd60c239d24be2ce0f8286a16918");
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_standard_graphic_hex()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var bmp = new QrCode(data).GetGraphic(10, "#000000", "#ffffff");

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("1e0afd60c239d24be2ce0f8286a16918");
    }


    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_standard_graphic_without_quietzones()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var bmp = new QrCode(data).GetGraphic(5, SKColors.Black, SKColors.White, false);

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("78f6af3170e47f3e930dfc05fa4f0cce");
    }


    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_with_transparent_logo_graphic()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new QrCode(data).GetGraphic(10, SKColors.Black, SKColors.Transparent, skiaBitmap);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("c20da0015d039b92e8e652183643f101");
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_with_non_transparent_logo_graphic()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new QrCode(data).GetGraphic(10, SKColors.Black, SKColors.White,
            skiaBitmap);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("478b8f52349924cbb067255b35e66df9");
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_with_logo_and_with_transparent_border()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new QrCode(data).GetGraphic(10, SKColors.Black, SKColors.Transparent, skiaBitmap, iconBorderWidth: 6);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("c20da0015d039b92e8e652183643f101");
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_with_logo_and_with_standard_border()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new QrCode(data).GetGraphic(10, SKColors.Black, SKColors.White, skiaBitmap, iconBorderWidth: 6);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("368aff889adc987645c8a362f90489bf");
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_create_qrcode_with_logo_and_with_custom_border()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);

        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new QrCode(data).GetGraphic(10, SKColors.Black, SKColors.Transparent, skiaBitmap, iconBorderWidth: 6,
            iconBackgroundColor: SKColors.DarkGreen);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("25eaa0b44828fb5f0f9632968f48d0b5");
    }


    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_instantate_qrcode_parameterless()
    {
        var svgCode = new QrCode();
        svgCode.ShouldNotBeNull();
        svgCode.ShouldBeOfType<QrCode>();
    }

    [Fact]
    [Category("QRRenderer/QrCode")]
    public void can_render_qrcode_from_helper()
    {
        //Create QR code                   
        var bmp = QrCodeHelper.GetQrCode("This is a quick test! 123#?", 10, SKColors.Black, SKColors.White,
            QrCodeGenerator.ECCLevel.H);

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("1e0afd60c239d24be2ce0f8286a16918");
    }
}