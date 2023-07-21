using BenchmarkDotNet.Attributes;
using QrSharp;

namespace QrSharpBenchmark;

public class AsciiQrCodeRendererBenchmark
{
    [Benchmark]
    public void render_ascii_qrcode()
    {
        var data = QrCodeGenerator.CreateQrCode("A05", QrCodeGenerator.ECCLevel.Q);
        new AsciiQrCode(data).GetGraphic(1);
    }

    [Benchmark]
    public void render_ascii_qrcode_without_quietzones()
    {
        var data = QrCodeGenerator.CreateQrCode("A05", QrCodeGenerator.ECCLevel.Q);
        new AsciiQrCode(data).GetGraphic(1, drawQuietZones: false);
    }

    [Benchmark]
    public void render_ascii_qrcode_with_custom_symbols()
    {
        var data = QrCodeGenerator.CreateQrCode("A", QrCodeGenerator.ECCLevel.Q);
        new AsciiQrCode(data).GetGraphic(2, "X", " ");
    }

    [Benchmark]
    public void render_ascii_qrcode_from_helper()
    {
        AsciiQrCodeHelper.GetQrCode("A", 2, "X", " ", QrCodeGenerator.ECCLevel.Q);
    }
}