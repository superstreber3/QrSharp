namespace QrSharp.Exceptions;

public class DataTooLongException : Exception
{
    public DataTooLongException(string eccLevel, string encodingMode, int maxSizeByte) : base(
        $"The given payload exceeds the maximum size of the Qr code standard. The maximum size allowed for the chosen parameters (ECC level={eccLevel}, EncodingMode={encodingMode}) is {maxSizeByte} byte."
    )
    {
    }

    public DataTooLongException(string eccLevel, string encodingMode, int version, int maxSizeByte) : base(
        $"The given payload exceeds the maximum size of the Qr code standard. The maximum size allowed for the chosen parameters (ECC level={eccLevel}, EncodingMode={encodingMode}, FixedVersion={version}) is {maxSizeByte} byte."
    )
    {
    }
}