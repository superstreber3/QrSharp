namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class PhoneNumber : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _number;

        /// <summary>
        ///     Generates a phone call payload
        /// </summary>
        /// <param name="number">Phone number of the receiver</param>
        public PhoneNumber(string number)
        {
            _number = number;
        }

        public override string ToString()
        {
            return $"tel:{_number}";
        }
    }
}