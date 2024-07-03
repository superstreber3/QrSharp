using QrSharp;
using QrSharpTest.Helpers;
using Shouldly;
using SkiaSharp;

namespace QrSharpTest;

public class SvgQrCodeRendererTest
{
    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_simple()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        var svg = new SvgQrCode(data).GetGraphic(5);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("5c251275a435a9aed7e591eb9c2e9949");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var svg = new SvgQrCode(data).GetGraphic(10, SKColors.Red, SKColors.White);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("1baa8c6ac3bd8c1eabcd2c5422dd9f78");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_viewbox_mode()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var svg = new SvgQrCode(data).GetGraphic(new SKSize(128, 128));
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("56719c7db39937c74377855a5dc4af0a");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_viewbox_mode_viewboxattr()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var svg = new SvgQrCode(data).GetGraphic(new SKSize(128, 128),
            sizingMode: SvgQrCode.SizingMode.ViewBoxAttribute);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("788afdb693b0b71eed344e495c180b60");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_without_quietzones()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var svg = new SvgQrCode(data).GetGraphic(10, SKColors.Red, SKColors.White, false);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("2a582427d86b51504c08ebcbcf0472bd");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_without_quietzones_hex()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var svg = new SvgQrCode(data).GetGraphic(10, "#000000", "#ffffff", false);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("4ab0417cc6127e347ca1b2322c49ed7d");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_with_png_logo()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);

        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909346
        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        var logoObj = new SvgQrCode.SvgLogo(skiaBitmap);
        logoObj.GetMediaType().ShouldBe(SvgQrCode.SvgLogo.MediaType.PNG);
        var svg = new SvgQrCode(data).GetGraphic(10, SKColors.DarkGray, SKColors.White, logo: logoObj);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("bba648df2cf54c80c10d96f3465593f0");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_with_svg_logo_embedded()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909361
        var logoSVG = File.ReadAllText(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_Scientist_2909361.svg"));
        var logoObj = new SvgQrCode.SvgLogo(logoSVG, 20);
        logoObj.GetMediaType().ShouldBe(SvgQrCode.SvgLogo.MediaType.SVG);
        var svg = new SvgQrCode(data).GetGraphic(10, SKColors.DarkGray, SKColors.White, logo: logoObj);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("855eb988d3af035abd273ed1629aa952");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_with_svg_logo_image_tag()
    {
        //Create QR code

        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        //Used logo is licensed under public domain. Ref.: https://thenounproject.com/Iconathon1/collection/redefining-women/?i=2909361
        var logoSvg = File.ReadAllText(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_Scientist_2909361.svg"));
        var logoObj = new SvgQrCode.SvgLogo(logoSvg, 20, iconEmbedded: false);
        var svg = new SvgQrCode(data).GetGraphic(10, SKColors.DarkGray, SKColors.White, logo: logoObj);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("bd442ea77d45a41a4f490b8d41591e04");
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_instantate_parameterless()
    {
        var svgCode = new SvgQrCode();
        svgCode.ShouldNotBeNull();
        svgCode.ShouldBeOfType<SvgQrCode>();
    }

    [Fact]
    [Category("QRRenderer/SvgQrCode")]
    public void can_render_svg_QrCode_from_helper()
    {
        //Create QR code                   
        var svg = SvgQrCodeHelper.GetQrCode("A", 2, "#000000", "#ffffff", QrCodeGenerator.ECCLevel.Q);
        var result = HelperFunctions.StringToHash(svg);
        result.ShouldBe("f5ec37aa9fb207e3701cc0d86c4a357d");
    }
}