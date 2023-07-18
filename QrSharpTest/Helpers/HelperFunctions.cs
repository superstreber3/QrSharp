using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace QrSharpTest.Helpers;

public static class HelperFunctions
{
    public static string GetAssemblyPath()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    public static string BitmapToHash(SKBitmap bitmap)
    {
        byte[] imgBytes = null;
        using (var image = SKImage.FromBitmap(bitmap))
        {
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                imgBytes = data.ToArray();
                var t = Convert.ToBase64String(imgBytes);
            }
        }

        return ByteArrayToHash(imgBytes);
    }

    public static string ByteArrayToHash(byte[] data)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }


    public static string StringToHash(string data)
    {
        return ByteArrayToHash(Encoding.UTF8.GetBytes(data));
    }
}