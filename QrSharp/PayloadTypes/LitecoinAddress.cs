using System.Globalization;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class LitecoinAddress
    {
        private readonly string _address;
        private readonly double _amount;
        private readonly string? _label, _message;

        public LitecoinAddress(string address, double amount, string? label = null, string? message = null)
        {
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
                    _amount.ToString("#.########", CultureInfo.InvariantCulture))
            };

            if (queryValues.Any(keyPair => !string.IsNullOrEmpty(keyPair.Value)))
            {
                query = "?" + string.Join("&", queryValues
                    .Where(keyPair => !string.IsNullOrEmpty(keyPair.Value))
                    .Select(keyPair => $"{keyPair.Key}={keyPair.Value}")
                    .ToArray());
            }

            return $"litecoin:{_address}{query}";
        }
    }
}