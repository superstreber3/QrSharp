using BenchmarkDotNet.Attributes;
using QrSharp;
using QrSharp.RenderTypes;

namespace QrSharpBenchmark;

public class PngByteQrCodeRendererBenchmark
{
    [Benchmark]
    public void render_pngbyte_qrcode_blackwhite()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        new PngByteQrCode(data).GetGraphic(5);
    }

    [Benchmark]
    public void render_pngbyte_qrcode_color()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 0, 0 }, new byte[] { 0, 0, 255 });
    }

    [Benchmark]
    public void render_pngbyte_qrcode_color_with_alpha()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 });
    }

    [Benchmark]
    public void render_pngbyte_qrcode_color_without_quietzones()
    {
        var data = QrCodeGenerator.CreateQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L);
        new PngByteQrCode(data).GetGraphic(5, new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 }, false);
    }

    [Benchmark]
    public void render_pngbyte_qrcode_from_helper()
    {
        PngByteQrCodeHelper.GetQrCode("This is a quick test! 123#?", QrCodeGenerator.ECCLevel.L, 10);
    }

    [Benchmark]
    public void render_pngbyte_qrcode_from_helper_2()
    {
        PngByteQrCodeHelper.GetQrCode("This is a quick test! 123#?", 5,
            new byte[] { 255, 255, 255, 127 }, new byte[] { 0, 0, 255 }, QrCodeGenerator.ECCLevel.L);
    }
}