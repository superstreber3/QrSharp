using QrSharp;
using QrSharpTest.Helpers;
using Shouldly;
using SkiaSharp;

namespace QrSharpTest;

public class ArtQrCodeRendererTest
{
    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_create_standard_Qrcode_graphic()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var bmp = new ArtQrCode(data).GetGraphic(10);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("9234123e0117497e920c389ddcd3bf3d");
    }

    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_create_standard_Qrcode_graphic_with_custom_finder()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var finder = new SKBitmap(15, 15);
        var bmp = new ArtQrCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.Transparent,
            finderPatternImage: finder);

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("81014b4802784fc5f14405ec6c25e3cc");
    }


    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_create_standard_Qrcode_graphic_without_quietzone()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var bmp = new ArtQrCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.Transparent,
            drawQuietZones: false);
        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("17b5fe4b8560a885e4b6183c7ffbdbe5");
    }

    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_create_standard_Qrcode_graphic_with_background()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);

        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var bmp = new ArtQrCode(data).GetGraphic(skiaBitmap);

        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("c04d96ee39062e930d82fafd63064364");
    }


    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void should_throw_pixelfactor_oor_exception()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var aCode = new ArtQrCode(data);

        var exception = Record.Exception(() =>
            aCode.GetGraphic(10, SKColors.Black, SKColors.White, SKColors.Transparent, pixelSizeFactor: 2));
        Assert.NotNull(exception);
        Assert.IsType<Exception>(exception);
        exception.Message.ShouldBe("The parameter pixelSize must be between 0 and 1. (0-100%)");
    }

    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_instantate_parameterless()
    {
        var asciiCode = new ArtQrCode();
        asciiCode.ShouldNotBeNull();
        asciiCode.ShouldBeOfType<ArtQrCode>();
    }

    [Fact]
    [Category("QrRenderer/ArtQrCode")]
    public void can_render_artQrcode_from_helper()
    {
        //Create Qr code
        var bmp = ArtQrCodeHelper.GetQrCode("A", 10, SKColors.Black, SKColors.White, SKColors.Transparent,
            QrCodeGenerator.ECCLevel.L);

        var result = HelperFunctions.BitmapToHash(bmp);
        result.ShouldBe("57a8b16033bdccebf3fd2244d82db0cc");
    }
}