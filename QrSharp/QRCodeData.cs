using System.Collections;
using System.IO.Compression;
using QrSharp.HelperMethods;

namespace QrSharp;

public class QrCodeData : IDisposable
{
    public List<BitArray> ModuleMatrix { get; set; }

    public QrCodeData(int version)
    {
        Version = version;
        var size = ModulesPerSideFromVersion(version);
        ModuleMatrix = new List<BitArray>();
        for (var i = 0; i < size; i++)
        {
            ModuleMatrix.Add(new BitArray(size));
        }
    }

    public QrCodeData(byte[] rawData, Compression compressMode)
    {
        var bytes = new List<byte>(rawData);

        switch (compressMode)
        {
            //Decompress
            case Compression.Deflate:
            {
                using var input = new MemoryStream(bytes.ToArray());
                using var output = new MemoryStream();
                using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
                {
                    StreamHelper.CopyTo(dstream, output);
                }

                bytes = new List<byte>(output.ToArray());

                break;
            }
            case Compression.GZip:
            {
                using var input = new MemoryStream(bytes.ToArray());
                using var output = new MemoryStream();
                using (var dstream = new GZipStream(input, CompressionMode.Decompress))
                {
                    StreamHelper.CopyTo(dstream, output);
                }

                bytes = new List<byte>(output.ToArray());

                break;
            }
            case Compression.Uncompressed:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(compressMode), compressMode, null);
        }

        if (bytes[0] != 0x51 || bytes[1] != 0x52 || bytes[2] != 0x52)
        {
            throw new Exception("Invalid raw data file. File type doesn't match \"QRR\".");
        }

        //Set Qr code version
        var sideLen = (int)bytes[4];
        bytes.RemoveRange(0, 5);
        Version = (sideLen - 21 - 8) / 4 + 1;

        //Unpack
        var modules = new Queue<bool>(8 * bytes.Count);
        foreach (var b in from b in bytes let bArr = new BitArray(new[] { b }) select b)
        {
            for (var i = 7; i >= 0; i--)
            {
                modules.Enqueue((b & (1 << i)) != 0);
            }
        }

        //Build module matrix
        ModuleMatrix = new List<BitArray>(sideLen);
        for (var y = 0; y < sideLen; y++)
        {
            ModuleMatrix.Add(new BitArray(sideLen));
            for (var x = 0; x < sideLen; x++)
            {
                ModuleMatrix[y][x] = modules.Dequeue();
            }
        }
    }

    public byte[] GetRawData(Compression compressMode)
    {
        var bytes = new List<byte>();

        //Add header - signature ("QRR")
        bytes.AddRange(new byte[] { 0x51, 0x52, 0x52, 0x00 });

        //Add header - row size
        bytes.Add((byte)ModuleMatrix.Count);

        //Build data queue
        var dataQueue = new Queue<int>();
        foreach (var row in ModuleMatrix)
        {
            foreach (var module in row)
            {
                dataQueue.Enqueue((bool)module ? 1 : 0);
            }
        }

        for (var i = 0; i < 8 - ModuleMatrix.Count * ModuleMatrix.Count % 8; i++)
        {
            dataQueue.Enqueue(0);
        }

        //Process queue
        while (dataQueue.Count > 0)
        {
            byte b = 0;
            for (var i = 7; i >= 0; i--)
            {
                b += (byte)(dataQueue.Dequeue() << i);
            }

            bytes.Add(b);
        }

        var rawData = bytes.ToArray();

        switch (compressMode)
        {
            //Compress stream (optional)
            case Compression.Deflate:
            {
                using var output = new MemoryStream();
                using (var dstream = new DeflateStream(output, CompressionMode.Compress))
                {
                    dstream.Write(rawData, 0, rawData.Length);
                }

                rawData = output.ToArray();

                break;
            }
            case Compression.GZip:
            {
                using var output = new MemoryStream();
                using (var gzipStream = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzipStream.Write(rawData, 0, rawData.Length);
                }

                rawData = output.ToArray();

                break;
            }
            case Compression.Uncompressed:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(compressMode), compressMode, null);
        }

        return rawData;
    }

    public int Version { get; private set; }

    private static int ModulesPerSideFromVersion(int version)
    {
        return 21 + (version - 1) * 4;
    }

    public void Dispose()
    {
        ModuleMatrix = null;
        Version = 0;
    }

    public enum Compression
    {
        Uncompressed,
        Deflate,
        GZip
    }
}