namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class SMS : QrSharp.PayloadGenerator.Payload
    {
        public enum SMSEncoding
        {
            SMS,
            SMSTO,

            // ReSharper disable once InconsistentNaming
            SMS_IOS
        }

        private readonly SMSEncoding _encoding;
        private readonly string _number, _subject;

        /// <summary>
        ///     Creates a SMS payload without text
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="encoding">Encoding type</param>
        public SMS(string number, SMSEncoding encoding = SMSEncoding.SMS)
        {
            _number = number;
            _subject = string.Empty;
            _encoding = encoding;
        }

        /// <summary>
        ///     Creates a SMS payload with text (subject)
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="subject">Text of the SMS</param>
        /// <param name="encoding">Encoding type</param>
        public SMS(string number, string subject, SMSEncoding encoding = SMSEncoding.SMS)
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
                case SMSEncoding.SMS:
                    var queryString = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryString = $"?body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"sms:{_number}{queryString}";
                    break;
                case SMSEncoding.SMS_IOS:
                    var queryStringIos = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringIos = $";body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"sms:{_number}{queryStringIos}";
                    break;
                case SMSEncoding.SMSTO:
                    returnVal = $"SMSTO:{_number}:{_subject}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }
}