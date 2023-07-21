using BenchmarkDotNet.Attributes;
using QrSharp;
using QrSharpTest.Helpers;
using SkiaSharp;

namespace QrSharpBenchmark;

public class ArtQrCodeRendererBenchmark
{
    [Benchmark]
    public void create_standard_qrcode_graphic()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        new ArtQrCode(data).GetGraphic(10);
    }

    [Benchmark]
    public void create_standard_Qrcode_graphic_with_custom_finder()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        var finder = new SKBitmap(15, 15);
        new ArtQrCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.Transparent,
            finderPatternImage: finder);
    }

    [Benchmark]
    public void create_standard_Qrcode_graphic_without_quietzone()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);
        new ArtQrCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.Transparent,
            drawQuietZones: false);
    }

    [Benchmark]
    public void create_standard_Qrcode_graphic_with_background()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.H);

        var stream = File.OpenRead(Path.Combine(HelperFunctions.GetAssemblyPath(), "assets",
            "noun_software engineer_2909346.png"));
        var skiaBitmap = SKBitmap.Decode(stream);
        new ArtQrCode(data).GetGraphic(skiaBitmap);
    }

    [Benchmark]
    public void render_artQrcode_from_helper()
    {
        ArtQrCodeHelper.GetQrCode("A", 10, SKColors.Black, SKColors.White, SKColors.Transparent,
            QrCodeGenerator.ECCLevel.L);
    }
}