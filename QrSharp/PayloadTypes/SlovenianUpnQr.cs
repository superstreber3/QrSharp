using System.Text;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class SlovenianUpnQr : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _amount, _code;
        private readonly string? _deadLine;

        private readonly string _payerAddress,
            _payerName,
            _payerPlace,
            _purpose,
            _recipientAddress,
            _recipientIban,
            _recipientName,
            _recipientPlace,
            _recipientSiModel,
            _recipientSiReference;

        //Keep in mind, that the ECC level has to be set to "M", version to 15 and ECI to EciMode.Iso8859_2 when generating a SlovenianUpnQr!
        //SlovenianUpnQr specification: https://www.upn-Qr.si/uploads/files/NavodilaZaProgramerjeUPNQr.pdf

        public SlovenianUpnQr(string payerName, string payerAddress, string payerPlace, string recipientName,
            string recipientAddress, string recipientPlace, string recipientIban, string description, double amount,
            string recipientSiModel = "SI00", string recipientSiReference = "", string code = "OTHR") :
            this(payerName, payerAddress, payerPlace, recipientName, recipientAddress, recipientPlace, recipientIban,
                description, amount, null, recipientSiModel, recipientSiReference, code)
        {
        }

        public SlovenianUpnQr(string payerName, string payerAddress, string payerPlace, string recipientName,
            string recipientAddress, string recipientPlace, string recipientIban, string description, double amount,
            DateTime? deadline, string recipientSiModel = "SI99", string recipientSiReference = "",
            string code = "OTHR")
        {
            _payerName = LimitLength(payerName.Trim(), 33);
            _payerAddress = LimitLength(payerAddress.Trim(), 33);
            _payerPlace = LimitLength(payerPlace.Trim(), 33);
            _amount = FormatAmount(amount);
            _code = LimitLength(code.Trim().ToUpper(), 4);
            _purpose = LimitLength(description.Trim(), 42);
            _deadLine = deadline is null ? "" : deadline.Value.ToString("dd.MM.yyyy");
            _recipientIban = LimitLength(recipientIban.Trim(), 34);
            _recipientName = LimitLength(recipientName.Trim(), 33);
            _recipientAddress = LimitLength(recipientAddress.Trim(), 33);
            _recipientPlace = LimitLength(recipientPlace.Trim(), 33);
            _recipientSiModel = LimitLength(recipientSiModel.Trim().ToUpper(), 4);
            _recipientSiReference = LimitLength(recipientSiReference.Trim(), 22);
        }

        public override int Version => 15;
        public override QrCodeGenerator.ECCLevel EccLevel => QrCodeGenerator.ECCLevel.M;
        public override QrCodeGenerator.EciMode EciMode => QrCodeGenerator.EciMode.Iso8859_2;

        private static string LimitLength(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }


        private static string FormatAmount(double amount)
        {
            var amt = (int)Math.Round(amount * 100.0);
            return $"{amt:00000000000}";
        }

        private int CalculateChecksum()
        {
            var cs = 5 + _payerName.Length; //5 = UPNQr constant Length
            cs += _payerAddress.Length;
            cs += _payerPlace.Length;
            cs += _amount.Length;
            cs += _code.Length;
            cs += _purpose.Length;
            if (_deadLine is not null)
            {
                cs += _deadLine.Length;
            }

            cs += _recipientIban.Length;
            cs += _recipientName.Length;
            cs += _recipientAddress.Length;
            cs += _recipientPlace.Length;
            cs += _recipientSiModel.Length;
            cs += _recipientSiReference.Length;
            cs += 19;
            return cs;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("UPNQr");
            sb.Append('\n').Append('\n').Append('\n').Append('\n').Append('\n');
            sb.Append(_payerName).Append('\n');
            sb.Append(_payerAddress).Append('\n');
            sb.Append(_payerPlace).Append('\n');
            sb.Append(_amount).Append('\n').Append('\n').Append('\n');
            sb.Append(_code.ToUpper()).Append('\n');
            sb.Append(_purpose).Append('\n');
            sb.Append(_deadLine).Append('\n');
            sb.Append(_recipientIban.ToUpper()).Append('\n');
            sb.Append(_recipientSiModel).Append(_recipientSiReference).Append('\n');
            sb.Append(_recipientName).Append('\n');
            sb.Append(_recipientAddress).Append('\n');
            sb.Append(_recipientPlace).Append('\n');
            sb.Append($"{CalculateChecksum():000}").Append('\n');
            return sb.ToString();
        }
    }
}