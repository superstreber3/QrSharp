namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class SkypeCall : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _skypeUsername;

        /// <summary>
        ///     Generates a Skype call payload
        /// </summary>
        /// <param name="skypeUsername">Skype username which will be called</param>
        public SkypeCall(string skypeUsername)
        {
            _skypeUsername = skypeUsername;
        }

        public override string ToString()
        {
            return $"skype:{_skypeUsername}?call";
        }
    }
}