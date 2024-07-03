using System.Text;
using QrSharp.HelperMethods;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class OneTimePassword : QrSharp.PayloadGenerator.Payload
    {
        public enum OneTimePasswordAuthAlgorithm
        {
            SHA1,
            SHA256,
            SHA512
        }

        public enum OneTimePasswordAuthType
        {
            TOTP,
            HOTP
        }

        //https://github.com/google/google-authenticator/wiki/Key-Uri-Format
        public OneTimePasswordAuthType Type { get; set; } = OneTimePasswordAuthType.TOTP;
        public string? Secret { get; set; }

        public OneTimePasswordAuthAlgorithm AuthAlgorithm { get; set; } = OneTimePasswordAuthAlgorithm.SHA1;

        public string? Issuer { get; set; }
        public string? Label { get; set; }
        public int Digits { get; set; } = 6;
        public int? Counter { get; set; } = null;
        public int? Period { get; set; } = 30;

        public override string ToString()
        {
            return Type switch
            {
                OneTimePasswordAuthType.TOTP => TimeToString(),
                OneTimePasswordAuthType.HOTP => HmacToString(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Note: Issuer:Label must only contain 1 : if either of the Issuer or the Label has a : then it is invalid.
        // Defaults are 6 digits and 30 for Period
        private string HmacToString()
        {
            var sb = new StringBuilder("otpauth://hotp/");
            ProcessCommonFields(sb);
            var actualCounter = Counter ?? 1;
            sb.Append("&counter=" + actualCounter);
            return sb.ToString();
        }

        private string TimeToString()
        {
            if (Period is null)
            {
                throw new Exception("Period must be set when using OneTimePasswordAuthType.TOTP");
            }

            var sb = new StringBuilder("otpauth://totp/");

            ProcessCommonFields(sb);

            if (Period != 30)
            {
                sb.Append("&period=" + Period);
            }

            return sb.ToString();
        }

        private void ProcessCommonFields(StringBuilder sb)
        {
            //check if Secret is null or whitespace
            if (StringHelper.IsNullOrWhiteSpace(Secret))
            {
                throw new Exception("Secret must be a filled out base32 encoded string");
            }

            var strippedSecret = Secret!.Replace(" ", "");
            string? escapedIssuer = null;
            string? label = null;

            if (!StringHelper.IsNullOrWhiteSpace(Issuer))
            {
                if (Issuer!.Contains(':'))
                {
                    throw new Exception("Issuer must not have a ':'");
                }

                escapedIssuer = Uri.EscapeDataString(Issuer);
            }

            if (!StringHelper.IsNullOrWhiteSpace(Label) && Label!.Contains(':'))
            {
                throw new Exception("Label must not have a ':'");
            }

            if (Label is not null && Issuer is not null)
            {
                label = Issuer + ":" + Label;
            }
            else if (Issuer is not null)
            {
                label = Issuer;
            }

            if (label is not null)
            {
                sb.Append(label);
            }

            sb.Append("?secret=" + strippedSecret);

            if (escapedIssuer is not null)
            {
                sb.Append("&issuer=" + escapedIssuer);
            }

            if (Digits != 6)
            {
                sb.Append("&digits=" + Digits);
            }
        }
    }
}