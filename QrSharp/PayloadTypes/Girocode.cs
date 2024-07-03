namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class Girocode : QrSharp.PayloadGenerator.Payload
    {
        public enum GirocodeEncoding
        {
            UTF_8,
            ISO_8859_1,
            ISO_8859_2,
            ISO_8859_4,
            ISO_8859_5,
            ISO_8859_7,
            ISO_8859_10,
            ISO_8859_15
        }

        public enum GirocodeVersion
        {
            Version1,
            Version2
        }

        public enum TypeOfRemittance
        {
            Structured,
            Unstructured
        }
        //Keep in mind, that the ECC level has to be set to "M" when generating a Girocode!
        //Girocode specification: http://www.europeanpaymentscouncil.eu/index.cfm/knowledge-bank/epc-documents/quick-response-code-guidelines-to-enable-data-capture-for-the-initiation-of-a-sepa-credit-transfer/epc069-12-quick-response-code-guidelines-to-enable-data-capture-for-the-initiation-of-a-sepa-credit-transfer1/

        private const string BR = "\n";

        private readonly decimal _amount;
        private readonly GirocodeEncoding _encoding;

        private readonly string _iban,
            _bic,
            _name,
            _purposeOfCreditTransfer,
            _remittanceInformation,
            _messageToGirocodeUser;

        private readonly TypeOfRemittance _typeOfRemittance;

        private readonly GirocodeVersion _version;


        /// <summary>
        ///     Generates the payload for a Girocode (Qr-Code with credit transfer information).
        ///     Attention: When using Girocode payload, Qr code must be generated with ECC level M!
        /// </summary>
        /// <param name="iban">Account number of the Beneficiary. Only IBAN is allowed.</param>
        /// <param name="bic">BIC of the Beneficiary Bank.</param>
        /// <param name="name">Name of the Beneficiary.</param>
        /// <param name="amount">
        ///     Amount of the Credit Transfer in Euro.
        ///     (Amount must be more than 0.01 and less than 999999999.99)
        /// </param>
        /// <param name="remittanceInformation">Remittance Information (Purpose-/reference text). (optional)</param>
        /// <param name="typeOfRemittance">
        ///     Type of remittance information. Either structured (e.g. ISO 11649 RF Creditor Reference)
        ///     and max. 35 chars or unstructured and max. 140 chars.
        /// </param>
        /// <param name="purposeOfCreditTransfer">Purpose of the Credit Transfer (optional)</param>
        /// <param name="messageToGirocodeUser">Beneficiary to originator information. (optional)</param>
        /// <param name="version">Girocode version. Either 001 or 002. Default: 001.</param>
        /// <param name="encoding">Encoding of the Girocode payload. Default: ISO-8859-1</param>
        public Girocode(string iban, string bic, string name, decimal amount, string remittanceInformation = "",
            TypeOfRemittance typeOfRemittance = TypeOfRemittance.Unstructured, string purposeOfCreditTransfer = "",
            string messageToGirocodeUser = "", GirocodeVersion version = GirocodeVersion.Version1,
            GirocodeEncoding encoding = GirocodeEncoding.ISO_8859_1)
        {
            _version = version;
            _encoding = encoding;
            if (!QrSharp.PayloadGenerator.IsValidIban(iban))
            {
                throw new GirocodeException("The IBAN entered isn't valid.");
            }

            _iban = iban.Replace(" ", "").ToUpper();
            if (!QrSharp.PayloadGenerator.IsValidBic(bic))
            {
                throw new GirocodeException("The BIC entered isn't valid.");
            }

            _bic = bic.Replace(" ", "").ToUpper();
            if (name.Length > 70)
            {
                throw new GirocodeException("(Payee-)Name must be shorter than 71 chars.");
            }

            _name = name;
            if (amount.ToString().Replace(",", ".").Contains('.') &&
                amount.ToString().Replace(",", ".").Split('.')[1].TrimEnd('0').Length > 2)
            {
                throw new GirocodeException("Amount must have less than 3 digits after decimal point.");
            }

            if (amount is < 0.01m or > 999999999.99m)
            {
                throw new GirocodeException(
                    "Amount has to at least 0.01 and must be smaller or equal to 999999999.99.");
            }

            _amount = amount;
            if (purposeOfCreditTransfer.Length > 4)
            {
                throw new GirocodeException("Purpose of credit transfer can only have 4 chars at maximum.");
            }

            _purposeOfCreditTransfer = purposeOfCreditTransfer;
            switch (typeOfRemittance)
            {
                case TypeOfRemittance.Unstructured when remittanceInformation.Length > 140:
                    throw new GirocodeException("Unstructured reference texts have to shorter than 141 chars.");
                case TypeOfRemittance.Structured when remittanceInformation.Length > 35:
                    throw new GirocodeException("Structured reference texts have to shorter than 36 chars.");
            }

            _typeOfRemittance = typeOfRemittance;
            _remittanceInformation = remittanceInformation;
            if (messageToGirocodeUser.Length > 70)
            {
                throw new GirocodeException("Message to the Girocode-User reader texts have to shorter than 71 chars.");
            }

            _messageToGirocodeUser = messageToGirocodeUser;
        }

        public override string ToString()
        {
            var girocodePayload = "BCD" + BR;
            girocodePayload += (_version == GirocodeVersion.Version1 ? "001" : "002") + BR;
            girocodePayload += (int)_encoding + 1 + BR;
            girocodePayload += "SCT" + BR;
            girocodePayload += _bic + BR;
            girocodePayload += _name + BR;
            girocodePayload += _iban + BR;
            girocodePayload += $"EUR{_amount:0.00}".Replace(",", ".") + BR;
            girocodePayload += _purposeOfCreditTransfer + BR;
            girocodePayload += (_typeOfRemittance == TypeOfRemittance.Structured
                ? _remittanceInformation
                : string.Empty) + BR;
            girocodePayload += (_typeOfRemittance == TypeOfRemittance.Unstructured
                ? _remittanceInformation
                : string.Empty) + BR;
            girocodePayload += _messageToGirocodeUser;

            return QrSharp.PayloadGenerator.ConvertStringToEncoding(girocodePayload,
                _encoding.ToString().Replace("_", "-"));
        }

        public class GirocodeException : Exception
        {
            public GirocodeException()
            {
            }

            public GirocodeException(string message)
                : base(message)
            {
            }

            public GirocodeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}