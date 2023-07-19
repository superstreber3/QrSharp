namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class MoneroTransaction : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _address;
        private readonly float? _txAmount;
        private readonly string? _txPaymentId, _recipientName, _txDescription;

        /// <summary>
        ///     Creates a monero transaction payload
        /// </summary>
        /// <param name="address">Receiver's monero address</param>
        /// <param name="txAmount">Amount to transfer</param>
        /// <param name="txPaymentId">Payment id</param>
        /// <param name="recipientName">Recipient's name</param>
        /// <param name="txDescription">Reference text / payment description</param>
        public MoneroTransaction(string address, float? txAmount = null, string? txPaymentId = null,
            string? recipientName = null, string? txDescription = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new MoneroTransactionException("The address is mandatory and has to be set.");
            }

            _address = address;
            if (txAmount is <= 0)
            {
                throw new MoneroTransactionException("Value of 'txAmount' must be greater than 0.");
            }

            _txAmount = txAmount;
            _txPaymentId = txPaymentId;
            _recipientName = recipientName;
            _txDescription = txDescription;
        }

        public override string ToString()
        {
            var moneroUri =
                $"monero://{_address}{(!string.IsNullOrEmpty(_txPaymentId) || !string.IsNullOrEmpty(_recipientName) || !string.IsNullOrEmpty(_txDescription) || _txAmount is not null ? "?" : string.Empty)}";
            moneroUri += !string.IsNullOrEmpty(_txPaymentId)
                ? $"tx_payment_id={Uri.EscapeDataString(_txPaymentId)}&"
                : string.Empty;
            moneroUri += !string.IsNullOrEmpty(_recipientName)
                ? $"recipient_name={Uri.EscapeDataString(_recipientName)}&"
                : string.Empty;
            moneroUri += _txAmount is not null ? $"tx_amount={_txAmount.ToString()?.Replace(",", ".")}&" : string.Empty;
            moneroUri += !string.IsNullOrEmpty(_txDescription)
                ? $"tx_description={Uri.EscapeDataString(_txDescription)}"
                : string.Empty;
            return moneroUri.TrimEnd('&');
        }


        public class MoneroTransactionException : Exception
        {
            public MoneroTransactionException()
            {
            }

            public MoneroTransactionException(string message)
                : base(message)
            {
            }

            public MoneroTransactionException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}