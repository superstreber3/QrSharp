using System.Globalization;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class BitcoinLikeCryptoCurrencyAddress : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _address;
        private readonly double? _amount;
        private readonly BitcoinLikeCryptoCurrencyType _currencyType;
        private readonly string? _label, _message;

        /// <summary>
        ///     Generates a Bitcoin like crypto currency payment payload. Qr Codes with this payload can open a payment app.
        /// </summary>
        /// <param name="currencyType">Bitcoin like crypto currency address of the payment receiver</param>
        /// <param name="address">Bitcoin like crypto currency address of the payment receiver</param>
        /// <param name="amount">Amount of coins to transfer</param>
        /// <param name="label">Reference label</param>
        /// <param name="message">Reference text aka message</param>
        protected BitcoinLikeCryptoCurrencyAddress(BitcoinLikeCryptoCurrencyType currencyType, string address,
            double? amount, string? label = null, string? message = null)
        {
            _currencyType = currencyType;
            _address = address;

            if (!string.IsNullOrEmpty(label))
            {
                _label = Uri.EscapeDataString(label);
            }

            if (!string.IsNullOrEmpty(message))
            {
                _message = Uri.EscapeDataString(message);
            }

            _amount = amount;
        }

        public override string ToString()
        {
            string? query = null;

            var queryValues = new KeyValuePair<string, string?>[]
            {
                new("label", _label),
                new("message", _message),
                new("amount",
                    _amount?.ToString("#.########", CultureInfo.InvariantCulture))
            };

            if (queryValues.Any(keyPair => !string.IsNullOrEmpty(keyPair.Value)))
            {
                query = "?" + string.Join("&", queryValues
                    .Where(keyPair => !string.IsNullOrEmpty(keyPair.Value))
                    .Select(keyPair => $"{keyPair.Key}={keyPair.Value}")
                    .ToArray());
            }

            return $"{Enum.GetName(typeof(BitcoinLikeCryptoCurrencyType), _currencyType)?.ToLower()}:{_address}{query}";
        }

        protected enum BitcoinLikeCryptoCurrencyType
        {
            Bitcoin,
            BitcoinCash,
            Litecoin
        }
    }
}