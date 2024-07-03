using System.Globalization;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class BitcoinAddress
    {
        private readonly string _address;
        private readonly double? _amount;
        private readonly string? _label, _message, _lightning;

        /// <summary>
        ///     Generates a Bitcoin like crypto currency payment payload. Qr Codes with this payload can open a payment app.
        /// </summary>
        /// <param name="address">Bitcoin wallet address of the payment receiver</param>
        /// <param name="amount">Amount of coins to transfer</param>
        /// <param name="label">Reference label</param>
        /// <param name="message">Reference text aka message</param>
        /// <param name="lightning">add a BOLT 11 invoice or a BOLT 12 offer (https://bitcoinqr.dev)</param>
        public BitcoinAddress(string address, double? amount = null, string? label = null, string? message = null,
            string? lightning = null)
        {
            _address = address;
            _amount = amount;

            if (!string.IsNullOrEmpty(label))
            {
                _label = Uri.EscapeDataString(label);
            }

            if (!string.IsNullOrEmpty(message))
            {
                _message = Uri.EscapeDataString(message);
            }

            _lightning = lightning;
        }

        public override string ToString()
        {
            string? query = null;

            var queryValues = new KeyValuePair<string, string?>[]
            {
                new("label", _label),
                new("message", _message),
                new("amount",
                    _amount?.ToString("#.########", CultureInfo.InvariantCulture)),
                new("lightning", _lightning)
            };

            if (queryValues.Any(keyPair => !string.IsNullOrEmpty(keyPair.Value)))
            {
                query = "?" + string.Join("&", queryValues
                    .Where(keyPair => !string.IsNullOrEmpty(keyPair.Value))
                    .Select(keyPair => $"{keyPair.Key}={keyPair.Value}")
                    .ToArray());
            }

            return $"bitcoin:{_address}{query}";
        }
    }
}