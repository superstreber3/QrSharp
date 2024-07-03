namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class WiFi : QrSharp.PayloadGenerator.Payload
    {
        public enum Authentication
        {
            WEP,
            WPA,
            NoPass
        }

        private readonly bool _isHiddenSsid;
        private readonly string? _password;
        private readonly string _ssid, _authenticationMode;

        /// <summary>
        ///     Generates a WiFi payload. Scanned by a Qr Code scanner app, the device will connect to the WiFi.
        /// </summary>
        /// <param name="ssid">SSID of the WiFi network</param>
        /// <param name="password">Password of the WiFi network</param>
        /// <param name="authenticationMode">Authentication mode (WEP, WPA, WPA2)</param>
        /// <param name="isHiddenSsid">Set flag, if the WiFi network hides its SSID</param>
        /// <param name="escapeHexStrings">
        ///     Set flag, if ssid/password is delivered as HEX string. Note: May not be supported on IOS
        ///     devices.
        /// </param>
        public WiFi(string ssid, string? password, Authentication authenticationMode, bool isHiddenSsid = false,
            bool escapeHexStrings = true)
        {
            _ssid = QrSharp.PayloadGenerator.EscapeInput(ssid);
            _ssid = escapeHexStrings && QrSharp.PayloadGenerator.IsHexStyle(_ssid) ? "\"" + _ssid + "\"" : _ssid;
            _password = QrSharp.PayloadGenerator.EscapeInput(password);
            _password = escapeHexStrings && QrSharp.PayloadGenerator.IsHexStyle(_password)
                ? "\"" + _password + "\""
                : _password;
            _authenticationMode = authenticationMode switch
            {
                Authentication.WEP => "WEP",
                Authentication.WPA => "WPA",
                Authentication.NoPass => "nopass",
                _ => throw new ArgumentOutOfRangeException(nameof(authenticationMode), authenticationMode, null)
            };
            _isHiddenSsid = isHiddenSsid;
        }

        public override string ToString()
        {
            return
                $"WIFI:T:{_authenticationMode};S:{_ssid};P:{_password};{(_isHiddenSsid ? "H:true" : string.Empty)};";
        }
    }
}