namespace QrSharp.HelperMethods;

internal class StreamHelper
{
    public static void CopyTo(Stream input, Stream output)
    {
        var buffer = new byte[16 * 1024];
        int bytesRead;
        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, bytesRead);
        }
    }
}