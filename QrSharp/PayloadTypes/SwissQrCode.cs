using System.Text.RegularExpressions;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class SwissQrCode : QrSharp.PayloadGenerator.Payload
    {
        /// <summary>
        ///     ISO 4217 currency codes
        /// </summary>
        public enum Currency
        {
            CHF = 756,
            EUR = 978
        }
        //Keep in mind, that the ECC level has to be set to "M" when generating a SwissQrCode!
        //SwissQrCode specification: 
        //    - (de) https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-de.pdf
        //    - (en) https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-en.pdf
        //Changes between version 1.0 and 2.0: https://www.paymentstandards.ch/dam/downloads/change-documentation-Qrr-de.pdf

        private const string BR = "\r\n";

        private readonly AdditionalInformation _additionalInformation;
        private readonly string? _alternativeProcedure1, _alternativeProcedure2;
        private readonly decimal? _amount;
        private readonly Contact _creditor;
        private readonly Currency _currency;
        private readonly Contact? _debitor;
        private readonly Iban _iban;
        private readonly Reference _reference;

        /// <summary>
        ///     Generates the payload for a SwissQrCode v2.0. (Don't forget to use ECC-Level=M, EncodingMode=UTF-8 and to set the
        ///     Swiss flag icon to the final Qr code.)
        /// </summary>
        /// <param name="iban">IBAN object</param>
        /// <param name="currency">Currency (either EUR or CHF)</param>
        /// <param name="creditor">Creditor (payee) information</param>
        /// <param name="reference">Reference information</param>
        /// <param name="additionalInformation"></param>
        /// <param name="debitor">Debitor (payer) information</param>
        /// <param name="amount">Amount</param>
        /// <param name="requestedDateOfPayment">Requested date of debitor's payment</param>
        /// <param name="ultimateCreditor">
        ///     Ultimate creditor information (use only in consultation with your bank - for future use
        ///     only!)
        /// </param>
        /// <param name="alternativeProcedure1">Optional command for alternative processing mode - line 1</param>
        /// <param name="alternativeProcedure2">Optional command for alternative processing mode - line 2</param>
        public SwissQrCode(Iban iban, Currency currency, Contact creditor, Reference reference,
            AdditionalInformation? additionalInformation = null, Contact? debitor = null, decimal? amount = null,
            DateTime? requestedDateOfPayment = null, Contact? ultimateCreditor = null,
            string? alternativeProcedure1 = null, string? alternativeProcedure2 = null)
        {
            _iban = iban;
            _creditor = creditor;
            _additionalInformation = additionalInformation ?? new AdditionalInformation();

            if (amount is not null && amount.ToString()!.Length > 12)
            {
                throw new SwissQrCodeException("Amount (including decimals) must be shorter than 13 places.");
            }

            _amount = amount;

            _currency = currency;
            _debitor = debitor;

            switch (iban.IsQrIban)
            {
                case true when reference.RefType != Reference.ReferenceType.QRR:
                    throw new SwissQrCodeException("If Qr-IBAN is used, you have to choose \"QRR\" as reference type!");
                case false when reference.RefType == Reference.ReferenceType.QRR:
                    throw new SwissQrCodeException(
                        "If non Qr-IBAN is used, you have to choose either \"SCOR\" or \"NON\" as reference type!");
            }

            _reference = reference;

            if (alternativeProcedure1 is not null && alternativeProcedure1.Length > 100)
            {
                throw new SwissQrCodeException(
                    "Alternative procedure information block 1 must be shorter than 101 chars.");
            }

            _alternativeProcedure1 = alternativeProcedure1;
            if (alternativeProcedure2 is not null && alternativeProcedure2.Length > 100)
            {
                throw new SwissQrCodeException(
                    "Alternative procedure information block 2 must be shorter than 101 chars.");
            }

            if (alternativeProcedure2 is not null)
            {
                _alternativeProcedure2 = alternativeProcedure2;
            }
        }

        public override string ToString()
        {
            //Header "logical" element
            var swissQrCodePayload = "SPC" + BR; //QrType
            swissQrCodePayload += "0200" + BR; //Version
            swissQrCodePayload += "1" + BR; //Coding

            //CdtrInf "logical" element
            swissQrCodePayload += _iban + BR; //IBAN


            //Cdtr "logical" element
            swissQrCodePayload += _creditor.ToString();

            //UltmtCdtr "logical" element
            //Since version 2.0 ultimate creditor was marked as "for future use" and has to be delivered empty in any case!
            swissQrCodePayload += string.Concat(Enumerable.Repeat(BR, 7).ToArray());

            //CcyAmtDate "logical" element
            //Amount has to use . as decimal separator in any case. See https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-en.pdf page 27.
            swissQrCodePayload += (_amount is not null ? $"{_amount:0.00}".Replace(",", ".") : string.Empty) + BR; //Amt
            swissQrCodePayload += _currency + BR; //Ccy                
            //Removed in S-Qr version 2.0
            //SwissQrCodePayload += (requestedDateOfPayment != null ?  ((DateTime)requestedDateOfPayment).ToString("yyyy-MM-dd") : string.Empty) + br; //ReqdExctnDt

            //UltmtDbtr "logical" element
            if (_debitor is not null)
            {
                swissQrCodePayload += _debitor.ToString();
            }
            else
            {
                swissQrCodePayload += string.Concat(Enumerable.Repeat(BR, 7).ToArray());
            }


            //RmtInf "logical" element
            swissQrCodePayload += _reference.RefType + BR; //Tp
            swissQrCodePayload +=
                (!string.IsNullOrEmpty(_reference.ReferenceText) ? _reference.ReferenceText : string.Empty) + BR; //Ref


            //AddInf "logical" element
            swissQrCodePayload += (!string.IsNullOrEmpty(_additionalInformation.UnstructuredMessage)
                ? _additionalInformation.UnstructuredMessage
                : string.Empty) + BR; //Ustrd
            swissQrCodePayload += _additionalInformation.Trailer + BR; //Trailer
            swissQrCodePayload += (!string.IsNullOrEmpty(_additionalInformation.BillInformation)
                ? _additionalInformation.BillInformation
                : string.Empty) + BR; //StrdBkgInf

            //AltPmtInf "logical" element
            if (!string.IsNullOrEmpty(_alternativeProcedure1))
            {
                swissQrCodePayload += _alternativeProcedure1.Replace("\n", "") + BR; //AltPmt
            }

            if (!string.IsNullOrEmpty(_alternativeProcedure2))
            {
                swissQrCodePayload += _alternativeProcedure2.Replace("\n", "") + BR; //AltPmt
            }

            //S-Qr specification 2.0, chapter 4.2.3
            if (swissQrCodePayload.EndsWith(BR))
            {
                swissQrCodePayload = swissQrCodePayload.Remove(swissQrCodePayload.Length - BR.Length);
            }

            return swissQrCodePayload;
        }

        public class AdditionalInformation
        {
            private readonly string _unstructuredMessage, _billInformation;

            /// <summary>
            ///     Creates an additional information object. Both parameters are optional and must be shorter than 141 chars in
            ///     combination.
            /// </summary>
            /// <param name="unstructuredMessage">Unstructured text message</param>
            /// <param name="billInformation">Bill information</param>
            public AdditionalInformation(string unstructuredMessage = "", string billInformation = "")
            {
                if ((!string.IsNullOrEmpty(unstructuredMessage) ? unstructuredMessage.Length : 0) +
                    (!string.IsNullOrEmpty(billInformation) ? billInformation.Length : 0) > 140)
                {
                    throw new SwissQrCodeAdditionalInformationException(
                        "Unstructured message and bill information must be shorter than 141 chars in total/combined.");
                }

                _unstructuredMessage = unstructuredMessage;
                _billInformation = billInformation;
                Trailer = "EPD";
            }

            public string? UnstructuredMessage => !string.IsNullOrEmpty(_unstructuredMessage)
                ? _unstructuredMessage.Replace("\n", "")
                : null;

            public string? BillInformation =>
                !string.IsNullOrEmpty(_billInformation) ? _billInformation.Replace("\n", "") : null;

            public string Trailer { get; }


            public class SwissQrCodeAdditionalInformationException : Exception
            {
                public SwissQrCodeAdditionalInformationException()
                {
                }

                public SwissQrCodeAdditionalInformationException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeAdditionalInformationException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Reference
        {
            public enum ReferenceTextType
            {
                QrReference,
                CreditorReferenceIso11649
            }

            /// <summary>
            ///     Reference type. When using a Qr-IBAN you have to use either "QRR" or "SCOR"
            /// </summary>
            public enum ReferenceType
            {
                QRR,
                SCOR,
                NON
            }

            private readonly string? _reference;

            /// <summary>
            ///     Creates a reference object which must be passed to the SwissQrCode instance
            /// </summary>
            /// <param name="referenceType">Type of the reference (QRR, SCOR or NON)</param>
            /// <param name="reference">Reference text</param>
            /// <param name="referenceTextType">Type of the reference text (Qr-reference or Creditor Reference)</param>
            public Reference(ReferenceType referenceType, string? reference = null,
                ReferenceTextType? referenceTextType = null)
            {
                RefType = referenceType;
                if (referenceType == ReferenceType.NON && !string.IsNullOrEmpty(reference))
                {
                    throw new SwissQrCodeReferenceException(
                        "Reference is only allowed when referenceType not equals \"NON\"");
                }

                if (referenceType != ReferenceType.NON && !string.IsNullOrEmpty(reference) && referenceTextType is null)
                {
                    throw new SwissQrCodeReferenceException(
                        "You have to set an ReferenceTextType when using the reference text.");
                }

                _reference = referenceTextType switch
                {
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) && reference.Length > 27 =>
                        throw new SwissQrCodeReferenceException("Qr-references have to be shorter than 28 chars."),
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) &&
                                                       !Regex.IsMatch(reference, @"^[0-9]+$") =>
                        throw new SwissQrCodeReferenceException("Qr-reference must exist out of digits only."),
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) &&
                                                       !QrSharp.PayloadGenerator.ChecksumMod10(reference) =>
                        throw new SwissQrCodeReferenceException("Qr-references is invalid. Checksum error."),
                    ReferenceTextType.CreditorReferenceIso11649 when !string.IsNullOrEmpty(reference) &&
                                                                     reference.Length > 25 =>
                        throw new SwissQrCodeReferenceException(
                            "Creditor references (ISO 11649) have to be shorter than 26 chars."),
                    _ => reference
                };
            }

            public ReferenceType RefType { get; }

            public string? ReferenceText => !string.IsNullOrEmpty(_reference) ? _reference.Replace("\n", "") : null;

            public class SwissQrCodeReferenceException : Exception
            {
                public SwissQrCodeReferenceException()
                {
                }

                public SwissQrCodeReferenceException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeReferenceException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Iban
        {
            public enum IbanType
            {
                Iban,
                QrIban
            }

            private readonly string _iban;
            private readonly IbanType _ibanType;

            /// <summary>
            ///     IBAN object with type information
            /// </summary>
            /// <param name="iban">IBAN</param>
            /// <param name="ibanType">Type of IBAN (normal or Qr-IBAN)</param>
            public Iban(string iban, IbanType ibanType)
            {
                switch (ibanType)
                {
                    case IbanType.Iban when !QrSharp.PayloadGenerator.IsValidIban(iban):
                        throw new SwissQrCodeIbanException("The IBAN entered isn't valid.");
                    case IbanType.QrIban when !QrSharp.PayloadGenerator.IsValidQrIban(iban):
                        throw new SwissQrCodeIbanException("The Qr-IBAN entered isn't valid.");
                }

                if (!iban.StartsWith("CH") && !iban.StartsWith("LI"))
                {
                    throw new SwissQrCodeIbanException("The IBAN must start with \"CH\" or \"LI\".");
                }

                _iban = iban;
                _ibanType = ibanType;
            }

            public bool IsQrIban => _ibanType == IbanType.QrIban;

            public override string ToString()
            {
                return _iban.Replace("-", "").Replace("\n", "").Replace(" ", "");
            }

            public class SwissQrCodeIbanException : Exception
            {
                public SwissQrCodeIbanException()
                {
                }

                public SwissQrCodeIbanException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeIbanException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Contact
        {
            public enum AddressType
            {
                StructuredAddress,
                CombinedAddress
            }

            private const string BR = "\r\n";

            private readonly static HashSet<string> TwoLetterCodes = ValidTwoLetterCodes();
            private readonly AddressType _addressType;
            private readonly string _name, _country;
            private readonly string? _zipCode, _city, _streetOrAddressLine1, _houseNumberOrAddressLine2;

            private Contact(string name, string? zipCode, string? city, string country, string? streetOrAddressLine1,
                string? houseNumberOrAddressLine2, AddressType addressType)
            {
                //Pattern extracted from https://Qr-validation.iso-payments.ch as explained in https://github.com/codebude/QrCoder/issues/97
                const string charsetPattern =
                    @"^([a-zA-Z0-9\.,;:'\ \+\-/\(\)?\*\[\]\{\}\\`´~ ]|[!""#%&<>÷=@_$£]|[àáâäçèéêëìíîïñòóôöùúûüýßÀÁÂÄÇÈÉÊËÌÍÎÏÒÓÔÖÙÚÛÜÑ])*$";

                _addressType = addressType;

                if (string.IsNullOrEmpty(name))
                {
                    throw new SwissQrCodeContactException("Name must not be empty.");
                }

                if (name.Length > 70)
                {
                    throw new SwissQrCodeContactException("Name must be shorter than 71 chars.");
                }

                if (!Regex.IsMatch(name, charsetPattern))
                {
                    throw new SwissQrCodeContactException(
                        $"Name must match the following pattern as defined in pain.001: {charsetPattern}");
                }

                _name = name;

                if (AddressType.StructuredAddress == _addressType)
                {
                    if (!string.IsNullOrEmpty(streetOrAddressLine1) && streetOrAddressLine1.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Street must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(streetOrAddressLine1) &&
                        !Regex.IsMatch(streetOrAddressLine1, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Street must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _streetOrAddressLine1 = streetOrAddressLine1;

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) && houseNumberOrAddressLine2.Length > 16)
                    {
                        throw new SwissQrCodeContactException("House number must be shorter than 17 chars.");
                    }

                    _houseNumberOrAddressLine2 = houseNumberOrAddressLine2;
                }
                else
                {
                    if (!string.IsNullOrEmpty(streetOrAddressLine1) && streetOrAddressLine1.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Address line 1 must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(streetOrAddressLine1) &&
                        !Regex.IsMatch(streetOrAddressLine1, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Address line 1 must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _streetOrAddressLine1 = streetOrAddressLine1;

                    if (string.IsNullOrEmpty(houseNumberOrAddressLine2))
                    {
                        throw new SwissQrCodeContactException(
                            "Address line 2 must be provided for combined addresses (address line-based addresses).");
                    }

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) && houseNumberOrAddressLine2.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Address line 2 must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) &&
                        !Regex.IsMatch(houseNumberOrAddressLine2, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Address line 2 must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _houseNumberOrAddressLine2 = houseNumberOrAddressLine2;
                }

                if (AddressType.StructuredAddress == _addressType)
                {
                    if (string.IsNullOrEmpty(zipCode))
                    {
                        throw new SwissQrCodeContactException("Zip code must not be empty.");
                    }

                    if (zipCode.Length > 16)
                    {
                        throw new SwissQrCodeContactException("Zip code must be shorter than 17 chars.");
                    }

                    if (!Regex.IsMatch(zipCode, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Zip code must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _zipCode = zipCode;

                    if (string.IsNullOrEmpty(city))
                    {
                        throw new SwissQrCodeContactException("City must not be empty.");
                    }

                    if (city.Length > 35)
                    {
                        throw new SwissQrCodeContactException("City name must be shorter than 36 chars.");
                    }

                    if (!Regex.IsMatch(city, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"City name must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _city = city;
                }
                else
                {
                    _zipCode = _city = string.Empty;
                }

                if (!IsValidTwoLetterCode(country))
                {
                    throw new SwissQrCodeContactException(
                        "Country must be a valid \"two letter\" country code as defined by  ISO 3166-1, but it isn't.");
                }

                _country = country;
            }

            public static Contact WithStructuredAddress(string name, string zipCode, string city, string country,
                string? street = null, string? houseNumber = null)
            {
                return new Contact(name, zipCode, city, country, street, houseNumber, AddressType.StructuredAddress);
            }

            public static Contact WithCombinedAddress(string name, string country, string addressLine1,
                string addressLine2)
            {
                return new Contact(name, null, null, country, addressLine1, addressLine2, AddressType.CombinedAddress);
            }

            private static bool IsValidTwoLetterCode(string code)
            {
                return TwoLetterCodes.Contains(code);
            }

            private static HashSet<string> ValidTwoLetterCodes()
            {
                string[] codes =
                {
                    "AF", "AL", "DZ", "AS", "AD", "AO", "AI", "AQ", "AG", "AR", "AM", "AW", "AU", "AT", "AZ", "BS",
                    "BH", "BD", "BB", "BY", "BE", "BZ", "BJ", "BM", "BT", "BO", "BQ", "BA", "BW", "BV", "BR", "IO",
                    "BN", "BG", "BF", "BI", "CV", "KH", "CM", "CA", "KY", "CF", "TD", "CL", "CN", "CX", "CC", "CO",
                    "KM", "CG", "CD", "CK", "CR", "CI", "HR", "CU", "CW", "CY", "CZ", "DK", "DJ", "DM", "DO", "EC",
                    "EG", "SV", "GQ", "ER", "EE", "SZ", "ET", "FK", "FO", "FJ", "FI", "FR", "GF", "PF", "TF", "GA",
                    "GM", "GE", "DE", "GH", "GI", "GR", "GL", "GD", "GP", "GU", "GT", "GG", "GN", "GW", "GY", "HT",
                    "HM", "VA", "HN", "HK", "HU", "IS", "IN", "ID", "IR", "IQ", "IE", "IM", "IL", "IT", "JM", "JP",
                    "JE", "JO", "KZ", "KE", "KI", "KP", "KR", "KW", "KG", "LA", "LV", "LB", "LS", "LR", "LY", "LI",
                    "LT", "LU", "MO", "MG", "MW", "MY", "MV", "ML", "MT", "MH", "MQ", "MR", "MU", "YT", "MX", "FM",
                    "MD", "MC", "MN", "ME", "MS", "MA", "MZ", "MM", "NA", "NR", "NP", "NL", "NC", "NZ", "NI", "NE",
                    "NG", "NU", "NF", "MP", "MK", "NO", "OM", "PK", "PW", "PS", "PA", "PG", "PY", "PE", "PH", "PN",
                    "PL", "PT", "PR", "QA", "RE", "RO", "RU", "RW", "BL", "SH", "KN", "LC", "MF", "PM", "VC", "WS",
                    "SM", "ST", "SA", "SN", "RS", "SC", "SL", "SG", "SX", "SK", "SI", "SB", "SO", "ZA", "GS", "SS",
                    "ES", "LK", "SD", "SR", "SJ", "SE", "CH", "SY", "TW", "TJ", "TZ", "TH", "TL", "TG", "TK", "TO",
                    "TT", "TN", "TR", "TM", "TC", "TV", "UG", "UA", "AE", "GB", "US", "UM", "UY", "UZ", "VU", "VE",
                    "VN", "VG", "VI", "WF", "EH", "YE", "ZM", "ZW", "AX"
                };
                return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                var contactData = $"{(AddressType.StructuredAddress == _addressType ? "S" : "K")}{BR}"; //AdrTp
                contactData += _name.Replace("\n", "") + BR; //Name
                contactData += (!string.IsNullOrEmpty(_streetOrAddressLine1)
                    ? _streetOrAddressLine1.Replace("\n", "")
                    : string.Empty) + BR; //StrtNmOrAdrLine1
                contactData += (!string.IsNullOrEmpty(_houseNumberOrAddressLine2)
                    ? _houseNumberOrAddressLine2.Replace("\n", "")
                    : string.Empty) + BR; //BldgNbOrAdrLine2
                contactData += _zipCode?.Replace("\n", "") + BR; //PstCd
                contactData += _city?.Replace("\n", "") + BR; //TwnNm
                contactData += _country + BR; //Ctry
                return contactData;
            }

            public class SwissQrCodeContactException : Exception
            {
                public SwissQrCodeContactException()
                {
                }

                public SwissQrCodeContactException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeContactException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class SwissQrCodeException : Exception
        {
            public SwissQrCodeException()
            {
            }

            public SwissQrCodeException(string message)
                : base(message)
            {
            }

            public SwissQrCodeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}