namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class MMS : QrSharp.PayloadGenerator.Payload
    {
        public enum MMSEncoding
        {
            MMS,
            MMSTO
        }

        private readonly MMSEncoding _encoding;
        private readonly string _number, _subject;

        /// <summary>
        ///     Creates a MMS payload without text
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="encoding">Encoding type</param>
        public MMS(string number, MMSEncoding encoding = MMSEncoding.MMS)
        {
            _number = number;
            _subject = string.Empty;
            _encoding = encoding;
        }

        /// <summary>
        ///     Creates a MMS payload with text (subject)
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="subject">Text of the MMS</param>
        /// <param name="encoding">Encoding type</param>
        public MMS(string number, string subject, MMSEncoding encoding = MMSEncoding.MMS)
        {
            _number = number;
            _subject = subject;
            _encoding = encoding;
        }

        public override string ToString()
        {
            string returnVal;
            switch (_encoding)
            {
                case MMSEncoding.MMSTO:
                    var queryStringMmsTo = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringMmsTo = $"?subject={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"mmsto:{_number}{queryStringMmsTo}";
                    break;
                case MMSEncoding.MMS:
                    var queryStringMms = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringMms = $"?body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"mms:{_number}{queryStringMms}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }
}