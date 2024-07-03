namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class Mail : QrSharp.PayloadGenerator.Payload
    {
        public enum MailEncoding
        {
            Mailto,
            Matmsg,
            Smtp
        }

        private readonly MailEncoding _encoding;
        private readonly string? _mailReceiver;
        private readonly string? _message;
        private readonly string? _subject;


        /// <summary>
        ///     Creates an email payload with subject and message/text
        /// </summary>
        /// <param name="mailReceiver">Receiver's email address</param>
        /// <param name="subject">Subject line of the email</param>
        /// <param name="message">Message content of the email</param>
        /// <param name="encoding">Payload encoding type. Choose dependent on your Qr Code scanner app.</param>
        public Mail(string? mailReceiver = null, string? subject = null, string? message = null,
            MailEncoding encoding = MailEncoding.Mailto)
        {
            _mailReceiver = mailReceiver;
            _subject = subject;
            _message = message;
            _encoding = encoding;
        }

        public override string ToString()
        {
            string returnVal;
            switch (_encoding)
            {
                case MailEncoding.Mailto:
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        parts.Add("subject=" + Uri.EscapeDataString(_subject));
                    }

                    if (!string.IsNullOrEmpty(_message))
                    {
                        parts.Add("body=" + Uri.EscapeDataString(_message));
                    }

                    var queryString = parts.Any() ? $"?{string.Join("&", parts.ToArray())}" : "";
                    returnVal = $"mailto:{_mailReceiver}{queryString}";
                    break;
                case MailEncoding.Matmsg:
                    returnVal =
                        $"MATMSG:TO:{_mailReceiver};SUB:{QrSharp.PayloadGenerator.EscapeInput(_subject)};BODY:{QrSharp.PayloadGenerator.EscapeInput(_message)};;";
                    break;
                case MailEncoding.Smtp:
                    returnVal =
                        $"SMTP:{_mailReceiver}:{QrSharp.PayloadGenerator.EscapeInput(_subject, true)}:{QrSharp.PayloadGenerator.EscapeInput(_message, true)}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }
}