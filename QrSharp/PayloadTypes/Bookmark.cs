namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class Bookmark : QrSharp.PayloadGenerator.Payload
    {
        private readonly string _url, _title;

        /// <summary>
        ///     Generates a bookmark payload. Scanned by an Qr Code reader, this one creates a browser bookmark.
        /// </summary>
        /// <param name="url">Url of the bookmark</param>
        /// <param name="title">Title of the bookmark</param>
        public Bookmark(string? url, string? title)
        {
            _url = QrSharp.PayloadGenerator.EscapeInput(url);
            _title = QrSharp.PayloadGenerator.EscapeInput(title);
        }

        public override string ToString()
        {
            return $"MEBKM:TITLE:{_title};URL:{_url};;";
        }
    }
}