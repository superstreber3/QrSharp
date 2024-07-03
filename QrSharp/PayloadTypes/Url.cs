namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class Url : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _url;

        /// <summary>
        ///     Generates a link. If not given, http/https protocol will be added.
        /// </summary>
        /// <param name="url">Link url target</param>
        public Url(string url)
        {
            _url = url;
        }

        public override string ToString()
        {
            return !_url.StartsWith("http") ? "http://" + _url : _url;
        }
    }
}