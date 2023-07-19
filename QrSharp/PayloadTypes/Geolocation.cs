namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class Geolocation : QrSharp.PayloadGenerator.Payload
    {
        public enum GeolocationEncoding
        {
            Geo,
            GoogleMaps
        }

        private readonly GeolocationEncoding _encoding;
        private readonly string _latitude, _longitude;

        /// <summary>
        ///     Generates a geo location payload. Supports raw location (GEO encoding) or Google Maps link (GoogleMaps encoding)
        /// </summary>
        /// <param name="latitude">Latitude with . as splitter</param>
        /// <param name="longitude">Longitude with . as splitter</param>
        /// <param name="encoding">Encoding type - GEO or GoogleMaps</param>
        public Geolocation(string latitude, string longitude, GeolocationEncoding encoding = GeolocationEncoding.Geo)
        {
            _latitude = latitude.Replace(",", ".");
            _longitude = longitude.Replace(",", ".");
            _encoding = encoding;
        }

        public override string ToString()
        {
            return _encoding switch
            {
                GeolocationEncoding.Geo => $"geo:{_latitude},{_longitude}",
                GeolocationEncoding.GoogleMaps => $"http://maps.google.com/maps?q={_latitude},{_longitude}",
                _ => "geo:"
            };
        }
    }
}