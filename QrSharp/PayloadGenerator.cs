using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QrSharp.HelperMethods;

namespace QrSharp;

public static class PayloadGenerator
{
    private static bool IsValidIban(string iban)
    {
        //Clean IBAN
        var ibanCleared = iban.ToUpper().Replace(" ", "").Replace("-", "");

        //Check for general structure
        var structurallyValid = Regex.IsMatch(ibanCleared, @"^[a-zA-Z]{2}[0-9]{2}([a-zA-Z0-9]?){16,30}$");

        //Check IBAN checksum
        var sum = $"{ibanCleared[4..]}{ibanCleared[..4]}".ToCharArray().Aggregate("",
            (current, c) => current + (char.IsLetter(c) ? (c - 55).ToString() : c.ToString()));
        var m = 0;
        for (var i = 0; i < (int)Math.Ceiling((sum.Length - 2) / 7d); i++)
        {
            var offset = i == 0 ? 0 : 2;
            var start = i * 7 + offset;
            var n = string.Concat(i == 0 ? "" : m.ToString(),
                sum.AsSpan(start, Math.Min(9 - offset, sum.Length - start)));
            if (!int.TryParse(n, NumberStyles.Any, CultureInfo.InvariantCulture, out m))
            {
                break;
            }

            m %= 97;
        }

        var checksumValid = m == 1;
        return structurallyValid && checksumValid;
    }

    private static bool IsValidQrIban(string iban)
    {
        var foundQrIid = false;
        try
        {
            var ibanCleared = iban.ToUpper().Replace(" ", "").Replace("-", "");
            var possibleQrIid = Convert.ToInt32(ibanCleared.Substring(4, 5));
            foundQrIid = possibleQrIid is >= 30000 and <= 31999;
        }
        catch
        {
            // ignored
        }

        return IsValidIban(iban) && foundQrIid;
    }

    private static bool IsValidBic(string bic)
    {
        return Regex.IsMatch(bic.Replace(" ", ""), @"^([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)$");
    }


    private static string ConvertStringToEncoding(string message, string encoding)
    {
        var iso = Encoding.GetEncoding(encoding);
        var utf8 = Encoding.UTF8;
        var utfBytes = utf8.GetBytes(message);
        var isoBytes = Encoding.Convert(utf8, iso, utfBytes);
        return iso.GetString(isoBytes, 0, isoBytes.Length);
    }

    private static string EscapeInput(string? inp, bool simple = false)
    {
        if (inp is null)
        {
            return "";
        }

        char[] forbiddenChars = { '\\', ';', ',', ':' };
        if (simple)
        {
            forbiddenChars = new[] { ':' };
        }

        return forbiddenChars.Aggregate(inp, (current, c) => current.Replace(c.ToString(), "\\" + c));
    }


    private static bool ChecksumMod10(string digits)
    {
        if (string.IsNullOrEmpty(digits) || digits.Length < 2)
        {
            return false;
        }

        int[] mods = { 0, 9, 4, 6, 8, 2, 7, 1, 3, 5 };

        var remainder = 0;
        for (var i = 0; i < digits.Length - 1; i++)
        {
            var num = Convert.ToInt32(digits[i]) - 48;
            remainder = mods[(num + remainder) % 10];
        }

        var checksum = (10 - remainder) % 10;
        return checksum == Convert.ToInt32(digits[^1]) - 48;
    }

    private static bool IsHexStyle(string inp)
    {
        return Regex.IsMatch(inp, @"\A\b[0-9a-fA-F]+\b\Z") || Regex.IsMatch(inp, @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z");
    }

    public abstract class Payload
    {
        public virtual int Version => -1;
        public virtual QrCodeGenerator.ECCLevel EccLevel => QrCodeGenerator.ECCLevel.M;
        public virtual QrCodeGenerator.EciMode EciMode => QrCodeGenerator.EciMode.Default;
        public abstract override string ToString();
    }

    public class WiFi : Payload
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
            _ssid = EscapeInput(ssid);
            _ssid = escapeHexStrings && IsHexStyle(_ssid) ? "\"" + _ssid + "\"" : _ssid;
            _password = EscapeInput(password);
            _password = escapeHexStrings && IsHexStyle(_password) ? "\"" + _password + "\"" : _password;
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

    public class Mail : Payload
    {
        public enum MailEncoding
        {
            Mailto,
            Matmsg,
            Smtp
        }

        private readonly MailEncoding _encoding;
        private readonly string? _mailReceiver;
        private readonly string? _message;
        private readonly string? _subject;


        /// <summary>
        ///     Creates an email payload with subject and message/text
        /// </summary>
        /// <param name="mailReceiver">Receiver's email address</param>
        /// <param name="subject">Subject line of the email</param>
        /// <param name="message">Message content of the email</param>
        /// <param name="encoding">Payload encoding type. Choose dependent on your Qr Code scanner app.</param>
        public Mail(string? mailReceiver = null, string? subject = null, string? message = null,
            MailEncoding encoding = MailEncoding.Mailto)
        {
            _mailReceiver = mailReceiver;
            _subject = subject;
            _message = message;
            _encoding = encoding;
        }

        public override string ToString()
        {
            string returnVal;
            switch (_encoding)
            {
                case MailEncoding.Mailto:
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        parts.Add("subject=" + Uri.EscapeDataString(_subject));
                    }

                    if (!string.IsNullOrEmpty(_message))
                    {
                        parts.Add("body=" + Uri.EscapeDataString(_message));
                    }

                    var queryString = parts.Any() ? $"?{string.Join("&", parts.ToArray())}" : "";
                    returnVal = $"mailto:{_mailReceiver}{queryString}";
                    break;
                case MailEncoding.Matmsg:
                    returnVal = $"MATMSG:TO:{_mailReceiver};SUB:{EscapeInput(_subject)};BODY:{EscapeInput(_message)};;";
                    break;
                case MailEncoding.Smtp:
                    returnVal = $"SMTP:{_mailReceiver}:{EscapeInput(_subject, true)}:{EscapeInput(_message, true)}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }

    public class SMS : Payload
    {
        public enum SMSEncoding
        {
            SMS,
            SMSTO,

            // ReSharper disable once InconsistentNaming
            SMS_IOS
        }

        private readonly SMSEncoding _encoding;
        private readonly string _number, _subject;

        /// <summary>
        ///     Creates a SMS payload without text
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="encoding">Encoding type</param>
        public SMS(string number, SMSEncoding encoding = SMSEncoding.SMS)
        {
            _number = number;
            _subject = string.Empty;
            _encoding = encoding;
        }

        /// <summary>
        ///     Creates a SMS payload with text (subject)
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="subject">Text of the SMS</param>
        /// <param name="encoding">Encoding type</param>
        public SMS(string number, string subject, SMSEncoding encoding = SMSEncoding.SMS)
        {
            _number = number;
            _subject = subject;
            _encoding = encoding;
        }

        public override string ToString()
        {
            string returnVal;
            switch (_encoding)
            {
                case SMSEncoding.SMS:
                    var queryString = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryString = $"?body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"sms:{_number}{queryString}";
                    break;
                case SMSEncoding.SMS_IOS:
                    var queryStringIos = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringIos = $";body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"sms:{_number}{queryStringIos}";
                    break;
                case SMSEncoding.SMSTO:
                    returnVal = $"SMSTO:{_number}:{_subject}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }

    public class MMS : Payload
    {
        public enum MMSEncoding
        {
            MMS,
            MMSTO
        }

        private readonly MMSEncoding _encoding;
        private readonly string _number, _subject;

        /// <summary>
        ///     Creates a MMS payload without text
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="encoding">Encoding type</param>
        public MMS(string number, MMSEncoding encoding = MMSEncoding.MMS)
        {
            _number = number;
            _subject = string.Empty;
            _encoding = encoding;
        }

        /// <summary>
        ///     Creates a MMS payload with text (subject)
        /// </summary>
        /// <param name="number">Receiver phone number</param>
        /// <param name="subject">Text of the MMS</param>
        /// <param name="encoding">Encoding type</param>
        public MMS(string number, string subject, MMSEncoding encoding = MMSEncoding.MMS)
        {
            _number = number;
            _subject = subject;
            _encoding = encoding;
        }

        public override string ToString()
        {
            string returnVal;
            switch (_encoding)
            {
                case MMSEncoding.MMSTO:
                    var queryStringMmsTo = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringMmsTo = $"?subject={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"mmsto:{_number}{queryStringMmsTo}";
                    break;
                case MMSEncoding.MMS:
                    var queryStringMms = string.Empty;
                    if (!string.IsNullOrEmpty(_subject))
                    {
                        queryStringMms = $"?body={Uri.EscapeDataString(_subject)}";
                    }

                    returnVal = $"mms:{_number}{queryStringMms}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return returnVal;
        }
    }

    public class Geolocation : Payload
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

    public class PhoneNumber : Payload
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

    public class SkypeCall : Payload
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

    public class Url : Payload
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


    public class WhatsAppMessage : Payload
    {
        private readonly string _number, _message;

        /// <summary>
        ///     Let's you compose a WhatApp message and send it the receiver number.
        /// </summary>
        /// <param name="number">
        ///     Receiver phone number where the
        ///     <number>
        ///         is a full phone number in international format.
        ///         Omit any zeroes, brackets, or dashes when adding the phone number in international format.
        ///         Use: 1XXXXXXXXXX | Don't use: +001-(XXX)XXXXXXX
        ///     </number>
        /// </param>
        /// <param name="message">The message</param>
        public WhatsAppMessage(string number, string message)
        {
            _number = number;
            _message = message;
        }

        /// <summary>
        ///     Let's you compose a WhatApp message. When scanned the user is asked to choose a contact who will receive the
        ///     message.
        /// </summary>
        /// <param name="message">The message</param>
        public WhatsAppMessage(string message)
        {
            _number = string.Empty;
            _message = message;
        }

        public override string ToString()
        {
            var cleanedPhone = Regex.Replace(_number, @"^[0+]+|[ ()-]", string.Empty);
            return $"https://wa.me/{cleanedPhone}?text={Uri.EscapeDataString(_message)}";
        }
    }


    public class Bookmark : Payload
    {
        private readonly string _url, _title;

        /// <summary>
        ///     Generates a bookmark payload. Scanned by an Qr Code reader, this one creates a browser bookmark.
        /// </summary>
        /// <param name="url">Url of the bookmark</param>
        /// <param name="title">Title of the bookmark</param>
        public Bookmark(string? url, string? title)
        {
            _url = EscapeInput(url);
            _title = EscapeInput(title);
        }

        public override string ToString()
        {
            return $"MEBKM:TITLE:{_title};URL:{_url};;";
        }
    }

    public class ContactData : Payload
    {
        /// <summary>
        ///     define the address format
        ///     Default: European format, ([Street] [House Number] and [Postal Code] [City]
        ///     Reversed: North American and others format ([House Number] [Street] and [City] [Postal Code])
        /// </summary>
        public enum AddressOrder
        {
            Default,
            Reversed
        }

        /// <summary>
        ///     Possible output types. Either vCard 2.1, vCard 3.0, vCard 4.0 or MeCard.
        /// </summary>
        public enum ContactOutputType
        {
            MeCard,
            VCard21,
            VCard3,
            VCard4
        }

        private readonly AddressOrder _addressOrder;
        private readonly DateTime? _birthday;
        private readonly string? _city;
        private readonly string? _country;
        private readonly string? _email;
        private readonly string _firstname;
        private readonly string? _houseNumber;
        private readonly string _lastname;
        private readonly string? _mobilePhone;
        private readonly string? _nickname;
        private readonly string? _note;
        private readonly string? _org;
        private readonly string? _orgTitle;
        private readonly ContactOutputType _outputType;
        private readonly string? _phone;
        private readonly string? _stateRegion;
        private readonly string? _street;
        private readonly string? _website;
        private readonly string? _workPhone;
        private readonly string? _zipCode;


        /// <summary>
        ///     Generates a vCard or meCard contact data set
        /// </summary>
        /// <param name="outputType">Payload output type</param>
        /// <param name="firstname">The firstname</param>
        /// <param name="lastname">The lastname</param>
        /// <param name="nickname">The display name</param>
        /// <param name="phone">Normal phone number</param>
        /// <param name="mobilePhone">Mobile phone</param>
        /// <param name="workPhone">Office phone number</param>
        /// <param name="email">E-Mail address</param>
        /// <param name="birthday">Birthday</param>
        /// <param name="website">Website / Homepage</param>
        /// <param name="street">Street</param>
        /// <param name="houseNumber">House number</param>
        /// <param name="city">City</param>
        /// <param name="stateRegion">State or Region</param>
        /// <param name="zipCode">Zip code</param>
        /// <param name="country">Country</param>
        /// <param name="addressOrder">The address order format to use</param>
        /// <param name="note">Memo text / notes</param>
        /// <param name="org">Organisation/Company</param>
        /// <param name="orgTitle">Organisation/Company Title</param>
        public ContactData(ContactOutputType outputType, string firstname, string lastname, string? nickname = null,
            string? phone = null, string? mobilePhone = null, string? workPhone = null, string? email = null,
            DateTime? birthday = null, string? website = null, string? street = null, string? houseNumber = null,
            string? city = null, string? zipCode = null, string? country = null, string? note = null,
            string? stateRegion = null, AddressOrder addressOrder = AddressOrder.Default, string? org = null,
            string? orgTitle = null)
        {
            _firstname = firstname;
            _lastname = lastname;
            _nickname = nickname;
            _org = org;
            _orgTitle = orgTitle;
            _phone = phone;
            _mobilePhone = mobilePhone;
            _workPhone = workPhone;
            _email = email;
            _birthday = birthday;
            _website = website;
            _street = street;
            _houseNumber = houseNumber;
            _city = city;
            _stateRegion = stateRegion;
            _zipCode = zipCode;
            _country = country;
            _addressOrder = addressOrder;
            _note = note;
            _outputType = outputType;
        }

        public override string ToString()
        {
            var payload = string.Empty;
            if (_outputType == ContactOutputType.MeCard)
            {
                payload += "MECARD+\r\n";
                if (!string.IsNullOrEmpty(_firstname) && !string.IsNullOrEmpty(_lastname))
                {
                    payload += $"N:{_lastname}, {_firstname}\r\n";
                }
                else if (!string.IsNullOrEmpty(_firstname) || !string.IsNullOrEmpty(_lastname))
                {
                    payload += $"N:{_firstname}{_lastname}\r\n";
                }

                if (!string.IsNullOrEmpty(_org))
                {
                    payload += $"ORG:{_org}\r\n";
                }

                if (!string.IsNullOrEmpty(_orgTitle))
                {
                    payload += $"TITLE:{_orgTitle}\r\n";
                }

                if (!string.IsNullOrEmpty(_phone))
                {
                    payload += $"TEL:{_phone}\r\n";
                }

                if (!string.IsNullOrEmpty(_mobilePhone))
                {
                    payload += $"TEL:{_mobilePhone}\r\n";
                }

                if (!string.IsNullOrEmpty(_workPhone))
                {
                    payload += $"TEL:{_workPhone}\r\n";
                }

                if (!string.IsNullOrEmpty(_email))
                {
                    payload += $"EMAIL:{_email}\r\n";
                }

                if (!string.IsNullOrEmpty(_note))
                {
                    payload += $"NOTE:{_note}\r\n";
                }

                if (_birthday is not null)
                {
                    payload += $"BDAY:{(DateTime)_birthday:yyyyMMdd}\r\n";
                }

                var addressString = _addressOrder == AddressOrder.Default
                    ? $"ADR:,,{(!string.IsNullOrEmpty(_street) ? _street + " " : "")}{(!string.IsNullOrEmpty(_houseNumber) ? _houseNumber : "")},{(!string.IsNullOrEmpty(_zipCode) ? _zipCode : "")},{(!string.IsNullOrEmpty(_city) ? _city : "")},{(!string.IsNullOrEmpty(_stateRegion) ? _stateRegion : "")},{(!string.IsNullOrEmpty(_country) ? _country : "")}\r\n"
                    : $"ADR:,,{(!string.IsNullOrEmpty(_houseNumber) ? _houseNumber + " " : "")}{(!string.IsNullOrEmpty(_street) ? _street : "")},{(!string.IsNullOrEmpty(_city) ? _city : "")},{(!string.IsNullOrEmpty(_stateRegion) ? _stateRegion : "")},{(!string.IsNullOrEmpty(_zipCode) ? _zipCode : "")},{(!string.IsNullOrEmpty(_country) ? _country : "")}\r\n";

                payload += addressString;
                if (!string.IsNullOrEmpty(_website))
                {
                    payload += $"URL:{_website}\r\n";
                }

                if (!string.IsNullOrEmpty(_nickname))
                {
                    payload += $"NICKNAME:{_nickname}\r\n";
                }

                payload = payload.Trim('\r', '\n');
            }
            else
            {
                var version = _outputType.ToString()[5..];
                if (version.Length > 1)
                {
                    version = version.Insert(1, ".");
                }
                else
                {
                    version += ".0";
                }

                payload += "BEGIN:VCARD\r\n";
                payload += $"VERSION:{version}\r\n";

                payload +=
                    $"N:{(!string.IsNullOrEmpty(_lastname) ? _lastname : "")};{(!string.IsNullOrEmpty(_firstname) ? _firstname : "")};;;\r\n";
                payload +=
                    $"FN:{(!string.IsNullOrEmpty(_firstname) ? _firstname + " " : "")}{(!string.IsNullOrEmpty(_lastname) ? _lastname : "")}\r\n";
                if (!string.IsNullOrEmpty(_org))
                {
                    payload += "ORG:" + _org + "\r\n";
                }

                if (!string.IsNullOrEmpty(_orgTitle))
                {
                    payload += "TITLE:" + _orgTitle + "\r\n";
                }

                if (!string.IsNullOrEmpty(_phone))
                {
                    payload += "TEL;";
                    payload += _outputType switch
                    {
                        ContactOutputType.VCard21 => $"HOME;VOICE:{_phone}",
                        ContactOutputType.VCard3 => $"TYPE=HOME,VOICE:{_phone}",
                        _ => $"TYPE=home,voice;VALUE=uri:tel:{_phone}"
                    };

                    payload += "\r\n";
                }

                if (!string.IsNullOrEmpty(_mobilePhone))
                {
                    payload += "TEL;";
                    payload += _outputType switch
                    {
                        ContactOutputType.VCard21 => $"HOME;CELL:{_mobilePhone}",
                        ContactOutputType.VCard3 => $"TYPE=HOME,CELL:{_mobilePhone}",
                        _ => $"TYPE=home,cell;VALUE=uri:tel:{_mobilePhone}"
                    };

                    payload += "\r\n";
                }

                if (!string.IsNullOrEmpty(_workPhone))
                {
                    payload += "TEL;";
                    payload += _outputType switch
                    {
                        ContactOutputType.VCard21 => $"WORK;VOICE:{_workPhone}",
                        ContactOutputType.VCard3 => $"TYPE=WORK,VOICE:{_workPhone}",
                        _ => $"TYPE=work,voice;VALUE=uri:tel:{_workPhone}"
                    };

                    payload += "\r\n";
                }


                payload += "ADR;";
                payload += _outputType switch
                {
                    ContactOutputType.VCard21 => "HOME;PREF:",
                    ContactOutputType.VCard3 => "TYPE=HOME,PREF:",
                    _ => "TYPE=home,pref:"
                };

                var addressString = _addressOrder == AddressOrder.Default
                    ? $";;{(!string.IsNullOrEmpty(_street) ? _street + " " : "")}{(!string.IsNullOrEmpty(_houseNumber) ? _houseNumber : "")};{(!string.IsNullOrEmpty(_zipCode) ? _zipCode : "")};{(!string.IsNullOrEmpty(_city) ? _city : "")};{(!string.IsNullOrEmpty(_stateRegion) ? _stateRegion : "")};{(!string.IsNullOrEmpty(_country) ? _country : "")}\r\n"
                    : $";;{(!string.IsNullOrEmpty(_houseNumber) ? _houseNumber + " " : "")}{(!string.IsNullOrEmpty(_street) ? _street : "")};{(!string.IsNullOrEmpty(_city) ? _city : "")};{(!string.IsNullOrEmpty(_stateRegion) ? _stateRegion : "")};{(!string.IsNullOrEmpty(_zipCode) ? _zipCode : "")};{(!string.IsNullOrEmpty(_country) ? _country : "")}\r\n";

                payload += addressString;

                if (_birthday is not null)
                {
                    payload += $"BDAY:{(DateTime)_birthday:yyyyMMdd}\r\n";
                }

                if (!string.IsNullOrEmpty(_website))
                {
                    payload += $"URL:{_website}\r\n";
                }

                if (!string.IsNullOrEmpty(_email))
                {
                    payload += $"EMAIL:{_email}\r\n";
                }

                if (!string.IsNullOrEmpty(_note))
                {
                    payload += $"NOTE:{_note}\r\n";
                }

                if (_outputType != ContactOutputType.VCard21 && !string.IsNullOrEmpty(_nickname))
                {
                    payload += $"NICKNAME:{_nickname}\r\n";
                }

                payload += "END:VCARD";
            }

            return payload;
        }
    }

    public class BitcoinLikeCryptoCurrencyAddress : Payload
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

    public class BitcoinAddress : BitcoinLikeCryptoCurrencyAddress
    {
        public BitcoinAddress(string address, double? amount, string? label = null, string? message = null)
            : base(BitcoinLikeCryptoCurrencyType.Bitcoin, address, amount, label, message)
        {
        }
    }

    public class BitcoinCashAddress : BitcoinLikeCryptoCurrencyAddress
    {
        public BitcoinCashAddress(string address, double? amount, string? label = null, string? message = null)
            : base(BitcoinLikeCryptoCurrencyType.BitcoinCash, address, amount, label, message)
        {
        }
    }

    public class LitecoinAddress : BitcoinLikeCryptoCurrencyAddress
    {
        public LitecoinAddress(string address, double? amount, string? label = null, string? message = null)
            : base(BitcoinLikeCryptoCurrencyType.Litecoin, address, amount, label, message)
        {
        }
    }

    public class SwissQrCode : Payload
    {
        /// <summary>
        ///     ISO 4217 currency codes
        /// </summary>
        public enum Currency
        {
            CHF = 756,
            EUR = 978
        }
        //Keep in mind, that the ECC level has to be set to "M" when generating a SwissQrCode!
        //SwissQrCode specification: 
        //    - (de) https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-de.pdf
        //    - (en) https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-en.pdf
        //Changes between version 1.0 and 2.0: https://www.paymentstandards.ch/dam/downloads/change-documentation-Qrr-de.pdf

        private const string BR = "\r\n";

        private readonly AdditionalInformation _additionalInformation;
        private readonly string? _alternativeProcedure1, _alternativeProcedure2;
        private readonly decimal? _amount;
        private readonly Contact _creditor;
        private readonly Currency _currency;
        private readonly Contact? _debitor;
        private readonly Iban _iban;
        private readonly Reference _reference;

        /// <summary>
        ///     Generates the payload for a SwissQrCode v2.0. (Don't forget to use ECC-Level=M, EncodingMode=UTF-8 and to set the
        ///     Swiss flag icon to the final Qr code.)
        /// </summary>
        /// <param name="iban">IBAN object</param>
        /// <param name="currency">Currency (either EUR or CHF)</param>
        /// <param name="creditor">Creditor (payee) information</param>
        /// <param name="reference">Reference information</param>
        /// <param name="additionalInformation"></param>
        /// <param name="debitor">Debitor (payer) information</param>
        /// <param name="amount">Amount</param>
        /// <param name="requestedDateOfPayment">Requested date of debitor's payment</param>
        /// <param name="ultimateCreditor">
        ///     Ultimate creditor information (use only in consultation with your bank - for future use
        ///     only!)
        /// </param>
        /// <param name="alternativeProcedure1">Optional command for alternative processing mode - line 1</param>
        /// <param name="alternativeProcedure2">Optional command for alternative processing mode - line 2</param>
        public SwissQrCode(Iban iban, Currency currency, Contact creditor, Reference reference,
            AdditionalInformation? additionalInformation = null, Contact? debitor = null, decimal? amount = null,
            DateTime? requestedDateOfPayment = null, Contact? ultimateCreditor = null,
            string? alternativeProcedure1 = null, string? alternativeProcedure2 = null)
        {
            _iban = iban;
            _creditor = creditor;
            _additionalInformation = additionalInformation ?? new AdditionalInformation();

            if (amount is not null && amount.ToString()!.Length > 12)
            {
                throw new SwissQrCodeException("Amount (including decimals) must be shorter than 13 places.");
            }

            _amount = amount;

            _currency = currency;
            _debitor = debitor;

            switch (iban.IsQrIban)
            {
                case true when reference.RefType != Reference.ReferenceType.QRR:
                    throw new SwissQrCodeException("If Qr-IBAN is used, you have to choose \"QRR\" as reference type!");
                case false when reference.RefType == Reference.ReferenceType.QRR:
                    throw new SwissQrCodeException(
                        "If non Qr-IBAN is used, you have to choose either \"SCOR\" or \"NON\" as reference type!");
            }

            _reference = reference;

            if (alternativeProcedure1 is not null && alternativeProcedure1.Length > 100)
            {
                throw new SwissQrCodeException(
                    "Alternative procedure information block 1 must be shorter than 101 chars.");
            }

            _alternativeProcedure1 = alternativeProcedure1;
            if (alternativeProcedure2 is not null && alternativeProcedure2.Length > 100)
            {
                throw new SwissQrCodeException(
                    "Alternative procedure information block 2 must be shorter than 101 chars.");
            }

            if (alternativeProcedure2 is not null)
            {
                _alternativeProcedure2 = alternativeProcedure2;
            }
        }

        public override string ToString()
        {
            //Header "logical" element
            var swissQrCodePayload = "SPC" + BR; //QrType
            swissQrCodePayload += "0200" + BR; //Version
            swissQrCodePayload += "1" + BR; //Coding

            //CdtrInf "logical" element
            swissQrCodePayload += _iban + BR; //IBAN


            //Cdtr "logical" element
            swissQrCodePayload += _creditor.ToString();

            //UltmtCdtr "logical" element
            //Since version 2.0 ultimate creditor was marked as "for future use" and has to be delivered empty in any case!
            swissQrCodePayload += string.Concat(Enumerable.Repeat(BR, 7).ToArray());

            //CcyAmtDate "logical" element
            //Amount has to use . as decimal separator in any case. See https://www.paymentstandards.ch/dam/downloads/ig-Qr-bill-en.pdf page 27.
            swissQrCodePayload += (_amount is not null ? $"{_amount:0.00}".Replace(",", ".") : string.Empty) + BR; //Amt
            swissQrCodePayload += _currency + BR; //Ccy                
            //Removed in S-Qr version 2.0
            //SwissQrCodePayload += (requestedDateOfPayment != null ?  ((DateTime)requestedDateOfPayment).ToString("yyyy-MM-dd") : string.Empty) + br; //ReqdExctnDt

            //UltmtDbtr "logical" element
            if (_debitor is not null)
            {
                swissQrCodePayload += _debitor.ToString();
            }
            else
            {
                swissQrCodePayload += string.Concat(Enumerable.Repeat(BR, 7).ToArray());
            }


            //RmtInf "logical" element
            swissQrCodePayload += _reference.RefType + BR; //Tp
            swissQrCodePayload +=
                (!string.IsNullOrEmpty(_reference.ReferenceText) ? _reference.ReferenceText : string.Empty) + BR; //Ref


            //AddInf "logical" element
            swissQrCodePayload += (!string.IsNullOrEmpty(_additionalInformation.UnstructuredMessage)
                ? _additionalInformation.UnstructuredMessage
                : string.Empty) + BR; //Ustrd
            swissQrCodePayload += _additionalInformation.Trailer + BR; //Trailer
            swissQrCodePayload += (!string.IsNullOrEmpty(_additionalInformation.BillInformation)
                ? _additionalInformation.BillInformation
                : string.Empty) + BR; //StrdBkgInf

            //AltPmtInf "logical" element
            if (!string.IsNullOrEmpty(_alternativeProcedure1))
            {
                swissQrCodePayload += _alternativeProcedure1.Replace("\n", "") + BR; //AltPmt
            }

            if (!string.IsNullOrEmpty(_alternativeProcedure2))
            {
                swissQrCodePayload += _alternativeProcedure2.Replace("\n", "") + BR; //AltPmt
            }

            //S-Qr specification 2.0, chapter 4.2.3
            if (swissQrCodePayload.EndsWith(BR))
            {
                swissQrCodePayload = swissQrCodePayload.Remove(swissQrCodePayload.Length - BR.Length);
            }

            return swissQrCodePayload;
        }

        public class AdditionalInformation
        {
            private readonly string _unstructuredMessage, _billInformation;

            /// <summary>
            ///     Creates an additional information object. Both parameters are optional and must be shorter than 141 chars in
            ///     combination.
            /// </summary>
            /// <param name="unstructuredMessage">Unstructured text message</param>
            /// <param name="billInformation">Bill information</param>
            public AdditionalInformation(string unstructuredMessage = "", string billInformation = "")
            {
                if ((!string.IsNullOrEmpty(unstructuredMessage) ? unstructuredMessage.Length : 0) +
                    (!string.IsNullOrEmpty(billInformation) ? billInformation.Length : 0) > 140)
                {
                    throw new SwissQrCodeAdditionalInformationException(
                        "Unstructured message and bill information must be shorter than 141 chars in total/combined.");
                }

                _unstructuredMessage = unstructuredMessage;
                _billInformation = billInformation;
                Trailer = "EPD";
            }

            public string? UnstructuredMessage => !string.IsNullOrEmpty(_unstructuredMessage)
                ? _unstructuredMessage.Replace("\n", "")
                : null;

            public string? BillInformation =>
                !string.IsNullOrEmpty(_billInformation) ? _billInformation.Replace("\n", "") : null;

            public string Trailer { get; }


            public class SwissQrCodeAdditionalInformationException : Exception
            {
                public SwissQrCodeAdditionalInformationException()
                {
                }

                public SwissQrCodeAdditionalInformationException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeAdditionalInformationException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Reference
        {
            public enum ReferenceTextType
            {
                QrReference,
                CreditorReferenceIso11649
            }

            /// <summary>
            ///     Reference type. When using a Qr-IBAN you have to use either "QRR" or "SCOR"
            /// </summary>
            public enum ReferenceType
            {
                QRR,
                SCOR,
                NON
            }

            private readonly string? _reference;

            /// <summary>
            ///     Creates a reference object which must be passed to the SwissQrCode instance
            /// </summary>
            /// <param name="referenceType">Type of the reference (QRR, SCOR or NON)</param>
            /// <param name="reference">Reference text</param>
            /// <param name="referenceTextType">Type of the reference text (Qr-reference or Creditor Reference)</param>
            public Reference(ReferenceType referenceType, string? reference = null,
                ReferenceTextType? referenceTextType = null)
            {
                RefType = referenceType;
                if (referenceType == ReferenceType.NON && !string.IsNullOrEmpty(reference))
                {
                    throw new SwissQrCodeReferenceException(
                        "Reference is only allowed when referenceType not equals \"NON\"");
                }

                if (referenceType != ReferenceType.NON && !string.IsNullOrEmpty(reference) && referenceTextType is null)
                {
                    throw new SwissQrCodeReferenceException(
                        "You have to set an ReferenceTextType when using the reference text.");
                }

                _reference = referenceTextType switch
                {
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) && reference.Length > 27 =>
                        throw new SwissQrCodeReferenceException("Qr-references have to be shorter than 28 chars."),
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) &&
                                                       !Regex.IsMatch(reference, @"^[0-9]+$") =>
                        throw new SwissQrCodeReferenceException("Qr-reference must exist out of digits only."),
                    ReferenceTextType.QrReference when !string.IsNullOrEmpty(reference) && !ChecksumMod10(reference) =>
                        throw new SwissQrCodeReferenceException("Qr-references is invalid. Checksum error."),
                    ReferenceTextType.CreditorReferenceIso11649 when !string.IsNullOrEmpty(reference) &&
                                                                     reference.Length > 25 =>
                        throw new SwissQrCodeReferenceException(
                            "Creditor references (ISO 11649) have to be shorter than 26 chars."),
                    _ => reference
                };
            }

            public ReferenceType RefType { get; }

            public string? ReferenceText => !string.IsNullOrEmpty(_reference) ? _reference.Replace("\n", "") : null;

            public class SwissQrCodeReferenceException : Exception
            {
                public SwissQrCodeReferenceException()
                {
                }

                public SwissQrCodeReferenceException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeReferenceException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Iban
        {
            public enum IbanType
            {
                Iban,
                QrIban
            }

            private readonly string _iban;
            private readonly IbanType _ibanType;

            /// <summary>
            ///     IBAN object with type information
            /// </summary>
            /// <param name="iban">IBAN</param>
            /// <param name="ibanType">Type of IBAN (normal or Qr-IBAN)</param>
            public Iban(string iban, IbanType ibanType)
            {
                switch (ibanType)
                {
                    case IbanType.Iban when !IsValidIban(iban):
                        throw new SwissQrCodeIbanException("The IBAN entered isn't valid.");
                    case IbanType.QrIban when !IsValidQrIban(iban):
                        throw new SwissQrCodeIbanException("The Qr-IBAN entered isn't valid.");
                }

                if (!iban.StartsWith("CH") && !iban.StartsWith("LI"))
                {
                    throw new SwissQrCodeIbanException("The IBAN must start with \"CH\" or \"LI\".");
                }

                _iban = iban;
                _ibanType = ibanType;
            }

            public bool IsQrIban => _ibanType == IbanType.QrIban;

            public override string ToString()
            {
                return _iban.Replace("-", "").Replace("\n", "").Replace(" ", "");
            }

            public class SwissQrCodeIbanException : Exception
            {
                public SwissQrCodeIbanException()
                {
                }

                public SwissQrCodeIbanException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeIbanException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class Contact
        {
            public enum AddressType
            {
                StructuredAddress,
                CombinedAddress
            }

            private const string BR = "\r\n";

            private static readonly HashSet<string> TwoLetterCodes = ValidTwoLetterCodes();
            private readonly AddressType _addressType;
            private readonly string _name, _country;
            private readonly string? _zipCode, _city, _streetOrAddressLine1, _houseNumberOrAddressLine2;

            private Contact(string name, string? zipCode, string? city, string country, string? streetOrAddressLine1,
                string? houseNumberOrAddressLine2, AddressType addressType)
            {
                //Pattern extracted from https://Qr-validation.iso-payments.ch as explained in https://github.com/codebude/QrCoder/issues/97
                const string charsetPattern =
                    @"^([a-zA-Z0-9\.,;:'\ \+\-/\(\)?\*\[\]\{\}\\`´~ ]|[!""#%&<>÷=@_$£]|[àáâäçèéêëìíîïñòóôöùúûüýßÀÁÂÄÇÈÉÊËÌÍÎÏÒÓÔÖÙÚÛÜÑ])*$";

                _addressType = addressType;

                if (string.IsNullOrEmpty(name))
                {
                    throw new SwissQrCodeContactException("Name must not be empty.");
                }

                if (name.Length > 70)
                {
                    throw new SwissQrCodeContactException("Name must be shorter than 71 chars.");
                }

                if (!Regex.IsMatch(name, charsetPattern))
                {
                    throw new SwissQrCodeContactException(
                        $"Name must match the following pattern as defined in pain.001: {charsetPattern}");
                }

                _name = name;

                if (AddressType.StructuredAddress == _addressType)
                {
                    if (!string.IsNullOrEmpty(streetOrAddressLine1) && streetOrAddressLine1.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Street must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(streetOrAddressLine1) &&
                        !Regex.IsMatch(streetOrAddressLine1, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Street must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _streetOrAddressLine1 = streetOrAddressLine1;

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) && houseNumberOrAddressLine2.Length > 16)
                    {
                        throw new SwissQrCodeContactException("House number must be shorter than 17 chars.");
                    }

                    _houseNumberOrAddressLine2 = houseNumberOrAddressLine2;
                }
                else
                {
                    if (!string.IsNullOrEmpty(streetOrAddressLine1) && streetOrAddressLine1.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Address line 1 must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(streetOrAddressLine1) &&
                        !Regex.IsMatch(streetOrAddressLine1, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Address line 1 must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _streetOrAddressLine1 = streetOrAddressLine1;

                    if (string.IsNullOrEmpty(houseNumberOrAddressLine2))
                    {
                        throw new SwissQrCodeContactException(
                            "Address line 2 must be provided for combined addresses (address line-based addresses).");
                    }

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) && houseNumberOrAddressLine2.Length > 70)
                    {
                        throw new SwissQrCodeContactException("Address line 2 must be shorter than 71 chars.");
                    }

                    if (!string.IsNullOrEmpty(houseNumberOrAddressLine2) &&
                        !Regex.IsMatch(houseNumberOrAddressLine2, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Address line 2 must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _houseNumberOrAddressLine2 = houseNumberOrAddressLine2;
                }

                if (AddressType.StructuredAddress == _addressType)
                {
                    if (string.IsNullOrEmpty(zipCode))
                    {
                        throw new SwissQrCodeContactException("Zip code must not be empty.");
                    }

                    if (zipCode.Length > 16)
                    {
                        throw new SwissQrCodeContactException("Zip code must be shorter than 17 chars.");
                    }

                    if (!Regex.IsMatch(zipCode, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"Zip code must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _zipCode = zipCode;

                    if (string.IsNullOrEmpty(city))
                    {
                        throw new SwissQrCodeContactException("City must not be empty.");
                    }

                    if (city.Length > 35)
                    {
                        throw new SwissQrCodeContactException("City name must be shorter than 36 chars.");
                    }

                    if (!Regex.IsMatch(city, charsetPattern))
                    {
                        throw new SwissQrCodeContactException(
                            $"City name must match the following pattern as defined in pain.001: {charsetPattern}");
                    }

                    _city = city;
                }
                else
                {
                    _zipCode = _city = string.Empty;
                }

                if (!IsValidTwoLetterCode(country))
                {
                    throw new SwissQrCodeContactException(
                        "Country must be a valid \"two letter\" country code as defined by  ISO 3166-1, but it isn't.");
                }

                _country = country;
            }

            public static Contact WithStructuredAddress(string name, string zipCode, string city, string country,
                string? street = null, string? houseNumber = null)
            {
                return new Contact(name, zipCode, city, country, street, houseNumber, AddressType.StructuredAddress);
            }

            public static Contact WithCombinedAddress(string name, string country, string addressLine1,
                string addressLine2)
            {
                return new Contact(name, null, null, country, addressLine1, addressLine2, AddressType.CombinedAddress);
            }

            private static bool IsValidTwoLetterCode(string code)
            {
                return TwoLetterCodes.Contains(code);
            }

            private static HashSet<string> ValidTwoLetterCodes()
            {
                string[] codes =
                {
                    "AF", "AL", "DZ", "AS", "AD", "AO", "AI", "AQ", "AG", "AR", "AM", "AW", "AU", "AT", "AZ", "BS",
                    "BH", "BD", "BB", "BY", "BE", "BZ", "BJ", "BM", "BT", "BO", "BQ", "BA", "BW", "BV", "BR", "IO",
                    "BN", "BG", "BF", "BI", "CV", "KH", "CM", "CA", "KY", "CF", "TD", "CL", "CN", "CX", "CC", "CO",
                    "KM", "CG", "CD", "CK", "CR", "CI", "HR", "CU", "CW", "CY", "CZ", "DK", "DJ", "DM", "DO", "EC",
                    "EG", "SV", "GQ", "ER", "EE", "SZ", "ET", "FK", "FO", "FJ", "FI", "FR", "GF", "PF", "TF", "GA",
                    "GM", "GE", "DE", "GH", "GI", "GR", "GL", "GD", "GP", "GU", "GT", "GG", "GN", "GW", "GY", "HT",
                    "HM", "VA", "HN", "HK", "HU", "IS", "IN", "ID", "IR", "IQ", "IE", "IM", "IL", "IT", "JM", "JP",
                    "JE", "JO", "KZ", "KE", "KI", "KP", "KR", "KW", "KG", "LA", "LV", "LB", "LS", "LR", "LY", "LI",
                    "LT", "LU", "MO", "MG", "MW", "MY", "MV", "ML", "MT", "MH", "MQ", "MR", "MU", "YT", "MX", "FM",
                    "MD", "MC", "MN", "ME", "MS", "MA", "MZ", "MM", "NA", "NR", "NP", "NL", "NC", "NZ", "NI", "NE",
                    "NG", "NU", "NF", "MP", "MK", "NO", "OM", "PK", "PW", "PS", "PA", "PG", "PY", "PE", "PH", "PN",
                    "PL", "PT", "PR", "QA", "RE", "RO", "RU", "RW", "BL", "SH", "KN", "LC", "MF", "PM", "VC", "WS",
                    "SM", "ST", "SA", "SN", "RS", "SC", "SL", "SG", "SX", "SK", "SI", "SB", "SO", "ZA", "GS", "SS",
                    "ES", "LK", "SD", "SR", "SJ", "SE", "CH", "SY", "TW", "TJ", "TZ", "TH", "TL", "TG", "TK", "TO",
                    "TT", "TN", "TR", "TM", "TC", "TV", "UG", "UA", "AE", "GB", "US", "UM", "UY", "UZ", "VU", "VE",
                    "VN", "VG", "VI", "WF", "EH", "YE", "ZM", "ZW", "AX"
                };
                return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                var contactData = $"{(AddressType.StructuredAddress == _addressType ? "S" : "K")}{BR}"; //AdrTp
                contactData += _name.Replace("\n", "") + BR; //Name
                contactData += (!string.IsNullOrEmpty(_streetOrAddressLine1)
                    ? _streetOrAddressLine1.Replace("\n", "")
                    : string.Empty) + BR; //StrtNmOrAdrLine1
                contactData += (!string.IsNullOrEmpty(_houseNumberOrAddressLine2)
                    ? _houseNumberOrAddressLine2.Replace("\n", "")
                    : string.Empty) + BR; //BldgNbOrAdrLine2
                contactData += _zipCode?.Replace("\n", "") + BR; //PstCd
                contactData += _city?.Replace("\n", "") + BR; //TwnNm
                contactData += _country + BR; //Ctry
                return contactData;
            }

            public class SwissQrCodeContactException : Exception
            {
                public SwissQrCodeContactException()
                {
                }

                public SwissQrCodeContactException(string message)
                    : base(message)
                {
                }

                public SwissQrCodeContactException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }

        public class SwissQrCodeException : Exception
        {
            public SwissQrCodeException()
            {
            }

            public SwissQrCodeException(string message)
                : base(message)
            {
            }

            public SwissQrCodeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }

    public class Girocode : Payload
    {
        public enum GirocodeEncoding
        {
            UTF_8,
            ISO_8859_1,
            ISO_8859_2,
            ISO_8859_4,
            ISO_8859_5,
            ISO_8859_7,
            ISO_8859_10,
            ISO_8859_15
        }

        public enum GirocodeVersion
        {
            Version1,
            Version2
        }

        public enum TypeOfRemittance
        {
            Structured,
            Unstructured
        }
        //Keep in mind, that the ECC level has to be set to "M" when generating a Girocode!
        //Girocode specification: http://www.europeanpaymentscouncil.eu/index.cfm/knowledge-bank/epc-documents/quick-response-code-guidelines-to-enable-data-capture-for-the-initiation-of-a-sepa-credit-transfer/epc069-12-quick-response-code-guidelines-to-enable-data-capture-for-the-initiation-of-a-sepa-credit-transfer1/

        private const string BR = "\n";

        private readonly decimal _amount;
        private readonly GirocodeEncoding _encoding;

        private readonly string _iban,
            _bic,
            _name,
            _purposeOfCreditTransfer,
            _remittanceInformation,
            _messageToGirocodeUser;

        private readonly TypeOfRemittance _typeOfRemittance;

        private readonly GirocodeVersion _version;


        /// <summary>
        ///     Generates the payload for a Girocode (Qr-Code with credit transfer information).
        ///     Attention: When using Girocode payload, Qr code must be generated with ECC level M!
        /// </summary>
        /// <param name="iban">Account number of the Beneficiary. Only IBAN is allowed.</param>
        /// <param name="bic">BIC of the Beneficiary Bank.</param>
        /// <param name="name">Name of the Beneficiary.</param>
        /// <param name="amount">
        ///     Amount of the Credit Transfer in Euro.
        ///     (Amount must be more than 0.01 and less than 999999999.99)
        /// </param>
        /// <param name="remittanceInformation">Remittance Information (Purpose-/reference text). (optional)</param>
        /// <param name="typeOfRemittance">
        ///     Type of remittance information. Either structured (e.g. ISO 11649 RF Creditor Reference)
        ///     and max. 35 chars or unstructured and max. 140 chars.
        /// </param>
        /// <param name="purposeOfCreditTransfer">Purpose of the Credit Transfer (optional)</param>
        /// <param name="messageToGirocodeUser">Beneficiary to originator information. (optional)</param>
        /// <param name="version">Girocode version. Either 001 or 002. Default: 001.</param>
        /// <param name="encoding">Encoding of the Girocode payload. Default: ISO-8859-1</param>
        public Girocode(string iban, string bic, string name, decimal amount, string remittanceInformation = "",
            TypeOfRemittance typeOfRemittance = TypeOfRemittance.Unstructured, string purposeOfCreditTransfer = "",
            string messageToGirocodeUser = "", GirocodeVersion version = GirocodeVersion.Version1,
            GirocodeEncoding encoding = GirocodeEncoding.ISO_8859_1)
        {
            _version = version;
            _encoding = encoding;
            if (!IsValidIban(iban))
            {
                throw new GirocodeException("The IBAN entered isn't valid.");
            }

            _iban = iban.Replace(" ", "").ToUpper();
            if (!IsValidBic(bic))
            {
                throw new GirocodeException("The BIC entered isn't valid.");
            }

            _bic = bic.Replace(" ", "").ToUpper();
            if (name.Length > 70)
            {
                throw new GirocodeException("(Payee-)Name must be shorter than 71 chars.");
            }

            _name = name;
            if (amount.ToString().Replace(",", ".").Contains('.') &&
                amount.ToString().Replace(",", ".").Split('.')[1].TrimEnd('0').Length > 2)
            {
                throw new GirocodeException("Amount must have less than 3 digits after decimal point.");
            }

            if (amount is < 0.01m or > 999999999.99m)
            {
                throw new GirocodeException(
                    "Amount has to at least 0.01 and must be smaller or equal to 999999999.99.");
            }

            _amount = amount;
            if (purposeOfCreditTransfer.Length > 4)
            {
                throw new GirocodeException("Purpose of credit transfer can only have 4 chars at maximum.");
            }

            _purposeOfCreditTransfer = purposeOfCreditTransfer;
            switch (typeOfRemittance)
            {
                case TypeOfRemittance.Unstructured when remittanceInformation.Length > 140:
                    throw new GirocodeException("Unstructured reference texts have to shorter than 141 chars.");
                case TypeOfRemittance.Structured when remittanceInformation.Length > 35:
                    throw new GirocodeException("Structured reference texts have to shorter than 36 chars.");
            }

            _typeOfRemittance = typeOfRemittance;
            _remittanceInformation = remittanceInformation;
            if (messageToGirocodeUser.Length > 70)
            {
                throw new GirocodeException("Message to the Girocode-User reader texts have to shorter than 71 chars.");
            }

            _messageToGirocodeUser = messageToGirocodeUser;
        }

        public override string ToString()
        {
            var girocodePayload = "BCD" + BR;
            girocodePayload += (_version == GirocodeVersion.Version1 ? "001" : "002") + BR;
            girocodePayload += (int)_encoding + 1 + BR;
            girocodePayload += "SCT" + BR;
            girocodePayload += _bic + BR;
            girocodePayload += _name + BR;
            girocodePayload += _iban + BR;
            girocodePayload += $"EUR{_amount:0.00}".Replace(",", ".") + BR;
            girocodePayload += _purposeOfCreditTransfer + BR;
            girocodePayload += (_typeOfRemittance == TypeOfRemittance.Structured
                ? _remittanceInformation
                : string.Empty) + BR;
            girocodePayload += (_typeOfRemittance == TypeOfRemittance.Unstructured
                ? _remittanceInformation
                : string.Empty) + BR;
            girocodePayload += _messageToGirocodeUser;

            return ConvertStringToEncoding(girocodePayload, _encoding.ToString().Replace("_", "-"));
        }

        public class GirocodeException : Exception
        {
            public GirocodeException()
            {
            }

            public GirocodeException(string message)
                : base(message)
            {
            }

            public GirocodeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }

    public class BezahlCode : Payload
    {
        /// <summary>
        ///     Operation modes of the BezahlCode
        /// </summary>
        public enum AuthorityType
        {
            /// <summary>
            ///     Single SEPA payment (SEPA-Überweisung)
            /// </summary>
            SinglePaymentSepa,

            /// <summary>
            ///     Single SEPA debit (SEPA-Lastschrift)
            /// </summary>
            SingleDirectDebitSepa,

            /// <summary>
            ///     Periodic SEPA payment (SEPA-Dauerauftrag)
            /// </summary>
            PeriodicSinglePaymentSepa,

            /// <summary>
            ///     Contact data
            /// </summary>
            Contact,

            /// <summary>
            ///     Contact data V2
            /// </summary>
            ContactV2
        }

        /// <summary>
        ///     ISO 4217 currency codes
        /// </summary>
        public enum Currency
        {
            AED = 784,
            AFN = 971,
            ALL = 008,
            AMD = 051,
            ANG = 532,
            AOA = 973,
            ARS = 032,
            AUD = 036,
            AWG = 533,
            AZN = 944,
            BAM = 977,
            BBD = 052,
            BDT = 050,
            BGN = 975,
            BHD = 048,
            BIF = 108,
            BMD = 060,
            BND = 096,
            BOB = 068,
            BOV = 984,
            BRL = 986,
            BSD = 044,
            BTN = 064,
            BWP = 072,
            BYR = 974,
            BZD = 084,
            CAD = 124,
            CDF = 976,
            CHE = 947,
            CHF = 756,
            CHW = 948,
            CLF = 990,
            CLP = 152,
            CNY = 156,
            COP = 170,
            COU = 970,
            CRC = 188,
            CUC = 931,
            CUP = 192,
            CVE = 132,
            CZK = 203,
            DJF = 262,
            DKK = 208,
            DOP = 214,
            DZD = 012,
            EGP = 818,
            ERN = 232,
            ETB = 230,
            EUR = 978,
            FJD = 242,
            FKP = 238,
            GBP = 826,
            GEL = 981,
            GHS = 936,
            GIP = 292,
            GMD = 270,
            GNF = 324,
            GTQ = 320,
            GYD = 328,
            HKD = 344,
            HNL = 340,
            HRK = 191,
            HTG = 332,
            HUF = 348,
            IDR = 360,
            ILS = 376,
            INR = 356,
            IQD = 368,
            IRR = 364,
            ISK = 352,
            JMD = 388,
            JOD = 400,
            JPY = 392,
            KES = 404,
            KGS = 417,
            KHR = 116,
            KMF = 174,
            KPW = 408,
            KRW = 410,
            KWD = 414,
            KYD = 136,
            KZT = 398,
            LAK = 418,
            LBP = 422,
            LKR = 144,
            LRD = 430,
            LSL = 426,
            LYD = 434,
            MAD = 504,
            MDL = 498,
            MGA = 969,
            MKD = 807,
            MMK = 104,
            MNT = 496,
            MOP = 446,
            MRO = 478,
            MUR = 480,
            MVR = 462,
            MWK = 454,
            MXN = 484,
            MXV = 979,
            MYR = 458,
            MZN = 943,
            NAD = 516,
            NGN = 566,
            NIO = 558,
            NOK = 578,
            NPR = 524,
            NZD = 554,
            OMR = 512,
            PAB = 590,
            PEN = 604,
            PGK = 598,
            PHP = 608,
            PKR = 586,
            PLN = 985,
            PYG = 600,
            QAR = 634,
            RON = 946,
            RSD = 941,
            RUB = 643,
            RWF = 646,
            SAR = 682,
            SBD = 090,
            SCR = 690,
            SDG = 938,
            SEK = 752,
            SGD = 702,
            SHP = 654,
            SLL = 694,
            SOS = 706,
            SRD = 968,
            SSP = 728,
            STD = 678,
            SVC = 222,
            SYP = 760,
            SZL = 748,
            THB = 764,
            TJS = 972,
            TMT = 934,
            TND = 788,
            TOP = 776,
            TRY = 949,
            TTD = 780,
            TWD = 901,
            TZS = 834,
            UAH = 980,
            UGX = 800,
            USD = 840,
            USN = 997,
            UYI = 940,
            UYU = 858,
            UZS = 860,
            VEF = 937,
            VND = 704,
            VUV = 548,
            WST = 882,
            XAF = 950,
            XAG = 961,
            XAU = 959,
            XBA = 955,
            XBB = 956,
            XBC = 957,
            XBD = 958,
            XCD = 951,
            XDR = 960,
            XOF = 952,
            XPD = 964,
            XPF = 953,
            XPT = 962,
            XSU = 994,
            XTS = 963,
            XUA = 965,
            XXX = 999,
            YER = 886,
            ZAR = 710,
            ZMW = 967,
            ZWL = 932
        }

        private readonly decimal _amount;
        private readonly AuthorityType _authority;
        private readonly Currency _currency;

        private readonly DateTime _executionDate,
            _dateOfSignature,
            _periodicFirstExecutionDate,
            _periodicLastExecutionDate;

        private readonly string? _iban,
            _bic,
            _bnc,
            _account,
            _sepaReference,
            _reason,
            _creditorId,
            _mandateId,
            _periodicTimeunit;
        //BezahlCode specification: http://www.bezahlcode.de/wp-content/uploads/BezahlCode_TechDok.pdf

        private readonly string _name;

        private readonly int _periodicTimeunitRotation;


        /// <summary>
        ///     Constructor for contact data
        /// </summary>
        /// <param name="authority">Type of the bank transfer</param>
        /// <param name="name">Name of the receiver (Empfänger)</param>
        /// <param name="account">Bank account (Kontonummer)</param>
        /// <param name="bnc">Bank institute (Bankleitzahl)</param>
        /// <param name="iban">IBAN</param>
        /// <param name="bic">BIC</param>
        /// <param name="reason">Reason (Verwendungszweck)</param>
        public BezahlCode(AuthorityType authority, string name, string account = "", string bnc = "", string iban = "",
            string bic = "", string reason = "") : this(authority, name, account, bnc, iban, bic, 0, string.Empty, 0,
            null, null, string.Empty, string.Empty, null, reason, string.Empty, Currency.EUR, null, 1)
        {
        }

        /// <summary>
        ///     Constructor for SEPA payments
        /// </summary>
        /// <param name="authority">Type of the bank transfer</param>
        /// <param name="name">Name of the receiver (Empfänger)</param>
        /// <param name="iban">IBAN</param>
        /// <param name="bic">BIC</param>
        /// <param name="amount">Amount (Betrag)</param>
        /// <param name="periodicTimeunit">Unit of intervall for payment ('M' = monthly, 'W' = weekly)</param>
        /// <param name="periodicTimeunitRotation">Intervall for payment. This value is combined with 'periodicTimeunit'</param>
        /// <param name="periodicFirstExecutionDate">Date of first periodic execution</param>
        /// <param name="periodicLastExecutionDate">Date of last periodic execution</param>
        /// <param name="creditorId">Creditor id (Gläubiger ID)</param>
        /// <param name="mandateId">Manadate id (Mandatsreferenz)</param>
        /// <param name="dateOfSignature">Signature date (Erteilungsdatum des Mandats)</param>
        /// <param name="reason">Reason (Verwendungszweck)</param>
        /// <param name="sepaReference">SEPA reference (SEPA-Referenz)</param>
        /// <param name="currency">Currency (Währung)</param>
        /// <param name="executionDate">Execution date (Ausführungsdatum)</param>
        public BezahlCode(AuthorityType authority, string name, string iban, string bic, decimal amount,
            string periodicTimeunit = "", int periodicTimeunitRotation = 0, DateTime? periodicFirstExecutionDate = null,
            DateTime? periodicLastExecutionDate = null, string creditorId = "", string mandateId = "",
            DateTime? dateOfSignature = null, string reason = "", string sepaReference = "",
            Currency currency = Currency.EUR, DateTime? executionDate = null) : this(authority, name, string.Empty,
            string.Empty, iban, bic, amount, periodicTimeunit, periodicTimeunitRotation, periodicFirstExecutionDate,
            periodicLastExecutionDate, creditorId, mandateId, dateOfSignature, reason, sepaReference, currency,
            executionDate, 2)
        {
        }


        /// <summary>
        ///     Generic constructor. Please use specific (non-SEPA or SEPA) constructor
        /// </summary>
        /// <param name="authority">Type of the bank transfer</param>
        /// <param name="name">Name of the receiver (Empfänger)</param>
        /// <param name="account">Bank account (Kontonummer)</param>
        /// <param name="bnc">Bank institute (Bankleitzahl)</param>
        /// <param name="iban">IBAN</param>
        /// <param name="bic">BIC</param>
        /// <param name="amount">Amount (Betrag)</param>
        /// <param name="periodicTimeunit">Unit of intervall for payment ('M' = monthly, 'W' = weekly)</param>
        /// <param name="periodicTimeunitRotation">Intervall for payment. This value is combined with 'periodicTimeunit'</param>
        /// <param name="periodicFirstExecutionDate">Date of first periodic execution</param>
        /// <param name="periodicLastExecutionDate">Date of last periodic execution</param>
        /// <param name="creditorId">Creditor id (Gläubiger ID)</param>
        /// <param name="mandateId">Manadate id (Mandatsreferenz)</param>
        /// <param name="dateOfSignature">Signature date (Erteilungsdatum des Mandats)</param>
        /// <param name="reason">Reason (Verwendungszweck)</param>
        /// <param name="sepaReference">SEPA reference (SEPA-Referenz)</param>
        /// <param name="currency">Currency (Währung)</param>
        /// <param name="executionDate">Execution date (Ausführungsdatum)</param>
        /// <param name="internalMode">Only used for internal state handling</param>
        public BezahlCode(AuthorityType authority, string name, string account, string bnc, string iban, string bic,
            decimal amount, string? periodicTimeunit = null, int periodicTimeunitRotation = 0,
            DateTime? periodicFirstExecutionDate = null, DateTime? periodicLastExecutionDate = null,
            string? creditorId = null, string? mandateId = null, DateTime? dateOfSignature = null,
            string? reason = null, string? sepaReference = null, Currency currency = Currency.EUR,
            DateTime? executionDate = null, int internalMode = 0)
        {
            switch (internalMode)
            {
                //Loaded via "contact-constructor"
                case 1 when authority != AuthorityType.Contact && authority != AuthorityType.ContactV2:
                    throw new BezahlCodeException(
                        "The constructor without an amount may only ne used with authority types 'contact' and 'contact_v2'.");
                case 1 when authority == AuthorityType.Contact &&
                            (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(bnc)):
                    throw new BezahlCodeException(
                        "When using authority type 'contact' the parameters 'account' and 'bnc' must be set.");
                case 1:
                {
                    if (authority != AuthorityType.ContactV2)
                    {
                        var oldFilled = !string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(bnc);
                        var newFilled = !string.IsNullOrEmpty(iban) && !string.IsNullOrEmpty(bic);
                        if ((!oldFilled && !newFilled) || (oldFilled && newFilled))
                        {
                            throw new BezahlCodeException(
                                "When using authority type 'contact_v2' either the parameters 'account' and 'bnc' or the parameters 'iban' and 'bic' must be set. Leave the other parameter pair empty.");
                        }
                    }

                    break;
                }
                case 2 when authority != AuthorityType.PeriodicSinglePaymentSepa &&
                            authority != AuthorityType.SingleDirectDebitSepa &&
                            authority != AuthorityType.SinglePaymentSepa:
                    throw new BezahlCodeException(
                        "The constructor with 'iban' and 'bic' may only be used with 'SEPA' authority types. Either choose another authority type or switch constructor.");
                case 2 when authority == AuthorityType.PeriodicSinglePaymentSepa &&
                            (string.IsNullOrEmpty(periodicTimeunit) || periodicTimeunitRotation == 0):
                    throw new BezahlCodeException(
                        "When using 'PeriodicSinglePaymentSepa' as authority type, the parameters 'periodicTimeunit' and 'periodicTimeunitRotation' must be set.");
            }

            _authority = authority;

            if (name.Length > 70)
            {
                throw new BezahlCodeException("(Payee-)Name must be shorter than 71 chars.");
            }

            _name = name;

            if (reason is { Length: > 27 })
            {
                throw new BezahlCodeException("Reasons texts have to be shorter than 28 chars.");
            }

            _reason = reason;
            var oldWayFilled = !string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(bnc);
            var newWayFilled = !string.IsNullOrEmpty(iban) && !string.IsNullOrEmpty(bic);
            //Non-SEPA payment types
            if (authority == AuthorityType.Contact || (authority == AuthorityType.ContactV2 && oldWayFilled))
            {
                if (!Regex.IsMatch(account.Replace(" ", ""), @"^[0-9]{1,9}$"))
                {
                    throw new BezahlCodeException("The account entered isn't valid.");
                }

                _account = account.Replace(" ", "").ToUpper();
                if (!Regex.IsMatch(bnc.Replace(" ", ""), @"^[0-9]{1,9}$"))
                {
                    throw new BezahlCodeException("The bnc entered isn't valid.");
                }

                _bnc = bnc.Replace(" ", "").ToUpper();
            }

            //SEPA payment types
            if (authority is AuthorityType.PeriodicSinglePaymentSepa or AuthorityType.SingleDirectDebitSepa
                    or AuthorityType.SinglePaymentSepa or AuthorityType.ContactV2 && newWayFilled)
            {
                if (!IsValidIban(iban))
                {
                    throw new BezahlCodeException("The IBAN entered isn't valid.");
                }

                _iban = iban.Replace(" ", "").ToUpper();
                if (!IsValidBic(bic))
                {
                    throw new BezahlCodeException("The BIC entered isn't valid.");
                }

                _bic = bic.Replace(" ", "").ToUpper();

                if (authority != AuthorityType.ContactV2)
                {
                    if (sepaReference is { Length: > 35 })
                    {
                        throw new BezahlCodeException("SEPA reference texts have to be shorter than 36 chars.");
                    }

                    _sepaReference = sepaReference;

                    if (!string.IsNullOrEmpty(creditorId) && !Regex.IsMatch(creditorId.Replace(" ", ""),
                            @"^[a-zA-Z]{2,2}[0-9]{2,2}([A-Za-z0-9]|[\+|\?|/|\-|:|\(|\)|\.|,|']){3,3}([A-Za-z0-9]|[\+|\?|/|\-|:|\(|\)|\.|,|']){1,28}$"))
                    {
                        throw new BezahlCodeException("The creditorId entered isn't valid.");
                    }

                    _creditorId = creditorId;
                    if (!string.IsNullOrEmpty(mandateId) && !Regex.IsMatch(mandateId.Replace(" ", ""),
                            @"^([A-Za-z0-9]|[\+|\?|/|\-|:|\(|\)|\.|,|']){1,35}$"))
                    {
                        throw new BezahlCodeException("The mandateId entered isn't valid.");
                    }

                    _mandateId = mandateId;
                    if (dateOfSignature is not null)
                    {
                        _dateOfSignature = (DateTime)dateOfSignature;
                    }
                }
            }

            //Checks for all payment types
            if (authority is AuthorityType.Contact or AuthorityType.ContactV2)
            {
                return;
            }

            if (amount.ToString().Replace(",", ".").Contains('.') &&
                amount.ToString().Replace(",", ".").Split('.')[1].TrimEnd('0').Length > 2)
            {
                throw new BezahlCodeException("Amount must have less than 3 digits after decimal point.");
            }

            if (amount is < 0.01m or > 999999999.99m)
            {
                throw new BezahlCodeException(
                    "Amount has to at least 0.01 and must be smaller or equal to 999999999.99.");
            }

            _amount = amount;

            _currency = currency;

            if (executionDate is null)
            {
                _executionDate = DateTime.Now;
            }
            else
            {
                if (DateTime.Today.Ticks > executionDate.Value.Ticks)
                {
                    throw new BezahlCodeException("Execution date must be today or in future.");
                }

                _executionDate = (DateTime)executionDate;
            }

            if (authority != AuthorityType.PeriodicSinglePaymentSepa)
            {
                return;
            }

            if (periodicTimeunit?.ToUpper() != "M" && periodicTimeunit?.ToUpper() != "W")
            {
                throw new BezahlCodeException(
                    "The periodicTimeunit must be either 'M' (monthly) or 'W' (weekly).");
            }

            _periodicTimeunit = periodicTimeunit;
            if (periodicTimeunitRotation is < 1 or > 52)
            {
                throw new BezahlCodeException(
                    "The periodicTimeunitRotation must be 1 or greater. (It means repeat the payment every 'periodicTimeunitRotation' weeks/months.");
            }

            _periodicTimeunitRotation = periodicTimeunitRotation;
            if (periodicFirstExecutionDate is not null)
            {
                _periodicFirstExecutionDate = (DateTime)periodicFirstExecutionDate;
            }

            if (periodicLastExecutionDate is not null)
            {
                _periodicLastExecutionDate = (DateTime)periodicLastExecutionDate;
            }
        }

        public override string ToString()
        {
            var bezahlCodePayload = "bank://";
            bezahlCodePayload += _authority switch
            {
                AuthorityType.SinglePaymentSepa => "singlepaymentsepa?",
                AuthorityType.PeriodicSinglePaymentSepa => "periodicsinglepaymentsepa?",
                AuthorityType.SingleDirectDebitSepa => "singledirectdebitsepa?",
                AuthorityType.Contact => "contact?",
                AuthorityType.ContactV2 => "contact_v2?",
                _ => throw new ArgumentOutOfRangeException()
            };

            bezahlCodePayload += $"name={Uri.EscapeDataString(_name)}&";

            if (_authority != AuthorityType.Contact && _authority != AuthorityType.ContactV2)
            {
                //Handle what is same for all payments
                bezahlCodePayload += $"iban={_iban}&";
                bezahlCodePayload += $"bic={_bic}&";

                if (!string.IsNullOrEmpty(_sepaReference))
                {
                    bezahlCodePayload += $"separeference={Uri.EscapeDataString(_sepaReference)}&";
                }

                if (_authority == AuthorityType.SingleDirectDebitSepa)
                {
                    if (!string.IsNullOrEmpty(_creditorId))
                    {
                        bezahlCodePayload += $"creditorid={Uri.EscapeDataString(_creditorId)}&";
                    }

                    if (!string.IsNullOrEmpty(_mandateId))
                    {
                        bezahlCodePayload += $"mandateid={Uri.EscapeDataString(_mandateId)}&";
                    }

                    if (_dateOfSignature != DateTime.MinValue)
                    {
                        bezahlCodePayload += $"dateofsignature={_dateOfSignature:ddMMyyyy}&";
                    }
                }

                bezahlCodePayload += $"amount={_amount:0.00}&".Replace(".", ",");

                if (!string.IsNullOrEmpty(_reason))
                {
                    bezahlCodePayload += $"reason={Uri.EscapeDataString(_reason)}&";
                }

                bezahlCodePayload += $"currency={_currency}&";
                bezahlCodePayload += $"executiondate={_executionDate:ddMMyyyy}&";
                if (_authority != AuthorityType.PeriodicSinglePaymentSepa)
                {
                    return bezahlCodePayload.Trim('&');
                }

                bezahlCodePayload += $"periodictimeunit={_periodicTimeunit}&";
                bezahlCodePayload += $"periodictimeunitrotation={_periodicTimeunitRotation}&";
                if (_periodicFirstExecutionDate != DateTime.MinValue)
                {
                    bezahlCodePayload +=
                        $"periodicfirstexecutiondate={_periodicFirstExecutionDate:ddMMyyyy}&";
                }

                if (_periodicLastExecutionDate != DateTime.MinValue)
                {
                    bezahlCodePayload +=
                        $"periodiclastexecutiondate={_periodicLastExecutionDate:ddMMyyyy}&";
                }
            }
            else
            {
                switch (_authority)
                {
                    //Handle what is same for all contacts
                    case AuthorityType.Contact:
                        bezahlCodePayload += $"account={_account}&";
                        bezahlCodePayload += $"bnc={_bnc}&";
                        break;
                    case AuthorityType.ContactV2 when !string.IsNullOrEmpty(_account) && !string.IsNullOrEmpty(_bnc):
                        bezahlCodePayload += $"account={_account}&";
                        bezahlCodePayload += $"bnc={_bnc}&";
                        break;
                    case AuthorityType.ContactV2:
                        bezahlCodePayload += $"iban={_iban}&";
                        bezahlCodePayload += $"bic={_bic}&";
                        break;
                }

                if (!string.IsNullOrEmpty(_reason))
                {
                    bezahlCodePayload += $"reason={Uri.EscapeDataString(_reason)}&";
                }
            }

            return bezahlCodePayload.Trim('&');
        }

        public class BezahlCodeException : Exception
        {
            public BezahlCodeException()
            {
            }

            public BezahlCodeException(string message)
                : base(message)
            {
            }

            public BezahlCodeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }

    public class CalendarEvent : Payload
    {
        public enum EventEncoding
        {
            // ReSharper disable once InconsistentNaming
            iCalComplete,
            Universal
        }

        private readonly EventEncoding _encoding;
        private readonly string _subject, _description, _location, _start, _end;

        /// <summary>
        ///     Generates a calender entry/event payload.
        /// </summary>
        /// <param name="subject">Subject/title of the calender event</param>
        /// <param name="description">Description of the event</param>
        /// <param name="location">Location (lat:long or address) of the event</param>
        /// <param name="start">Start time of the event</param>
        /// <param name="end">End time of the event</param>
        /// <param name="allDayEvent">Is it a full day event?</param>
        /// <param name="encoding">Type of encoding (universal or iCal)</param>
        public CalendarEvent(string subject, string description, string location, DateTime start, DateTime end,
            bool allDayEvent, EventEncoding encoding = EventEncoding.Universal)
        {
            _subject = subject;
            _description = description;
            _location = location;
            _encoding = encoding;
            var dtFormat = allDayEvent ? "yyyyMMdd" : "yyyyMMddTHHmmss";
            _start = start.ToString(dtFormat);
            _end = end.ToString(dtFormat);
        }

        public override string ToString()
        {
            var vEvent = $"BEGIN:VEVENT{Environment.NewLine}";
            vEvent += $"SUMMARY:{_subject}{Environment.NewLine}";
            vEvent += !string.IsNullOrEmpty(_description) ? $"DESCRIPTION:{_description}{Environment.NewLine}" : "";
            vEvent += !string.IsNullOrEmpty(_location) ? $"LOCATION:{_location}{Environment.NewLine}" : "";
            vEvent += $"DTSTART:{_start}{Environment.NewLine}";
            vEvent += $"DTEND:{_end}{Environment.NewLine}";
            vEvent += "END:VEVENT";

            if (_encoding == EventEncoding.iCalComplete)
            {
                vEvent =
                    $@"BEGIN:VCALENDAR{Environment.NewLine}VERSION:2.0{Environment.NewLine}{vEvent}{Environment.NewLine}END:VCALENDAR";
            }

            return vEvent;
        }
    }

    public class OneTimePassword : Payload
    {
        public enum OneTimePasswordAuthAlgorithm
        {
            SHA1,
            SHA256,
            SHA512
        }

        public enum OneTimePasswordAuthType
        {
            TOTP,
            HOTP
        }

        //https://github.com/google/google-authenticator/wiki/Key-Uri-Format
        public OneTimePasswordAuthType Type { get; set; } = OneTimePasswordAuthType.TOTP;
        public string? Secret { get; set; }

        public OneTimePasswordAuthAlgorithm AuthAlgorithm { get; set; } = OneTimePasswordAuthAlgorithm.SHA1;

        public string? Issuer { get; set; }
        public string? Label { get; set; }
        public int Digits { get; set; } = 6;
        public int? Counter { get; set; } = null;
        public int? Period { get; set; } = 30;

        public override string ToString()
        {
            return Type switch
            {
                OneTimePasswordAuthType.TOTP => TimeToString(),
                OneTimePasswordAuthType.HOTP => HmacToString(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Note: Issuer:Label must only contain 1 : if either of the Issuer or the Label has a : then it is invalid.
        // Defaults are 6 digits and 30 for Period
        private string HmacToString()
        {
            var sb = new StringBuilder("otpauth://hotp/");
            ProcessCommonFields(sb);
            var actualCounter = Counter ?? 1;
            sb.Append("&counter=" + actualCounter);
            return sb.ToString();
        }

        private string TimeToString()
        {
            if (Period is null)
            {
                throw new Exception("Period must be set when using OneTimePasswordAuthType.TOTP");
            }

            var sb = new StringBuilder("otpauth://totp/");

            ProcessCommonFields(sb);

            if (Period != 30)
            {
                sb.Append("&period=" + Period);
            }

            return sb.ToString();
        }

        private void ProcessCommonFields(StringBuilder sb)
        {
            //check if Secret is null or whitespace
            if (StringHelper.IsNullOrWhiteSpace(Secret))
            {
                throw new Exception("Secret must be a filled out base32 encoded string");
            }

            var strippedSecret = Secret!.Replace(" ", "");
            string? escapedIssuer = null;
            string? label = null;

            if (!StringHelper.IsNullOrWhiteSpace(Issuer))
            {
                if (Issuer!.Contains(':'))
                {
                    throw new Exception("Issuer must not have a ':'");
                }

                escapedIssuer = Uri.EscapeDataString(Issuer);
            }

            if (!StringHelper.IsNullOrWhiteSpace(Label) && Label!.Contains(':'))
            {
                throw new Exception("Label must not have a ':'");
            }

            if (Label is not null && Issuer is not null)
            {
                label = Issuer + ":" + Label;
            }
            else if (Issuer is not null)
            {
                label = Issuer;
            }

            if (label is not null)
            {
                sb.Append(label);
            }

            sb.Append("?secret=" + strippedSecret);

            if (escapedIssuer is not null)
            {
                sb.Append("&issuer=" + escapedIssuer);
            }

            if (Digits != 6)
            {
                sb.Append("&digits=" + Digits);
            }
        }
    }

    public class ShadowSocksConfig : Payload
    {
        public enum Method
        {
            // AEAD
            Chacha20IetfPoly1305,
            Aes128Gcm,
            Aes192Gcm,
            Aes256Gcm,

            // AEAD, not standard
            XChacha20IetfPoly1305,

            // Stream cipher
            Aes128Cfb,
            Aes192Cfb,
            Aes256Cfb,
            Aes128Ctr,
            Aes192Ctr,
            Aes256Ctr,
            Camellia128Cfb,
            Camellia192Cfb,
            Camellia256Cfb,
            Chacha20Ietf,

            // alias of Aes256Cfb
            Aes256Cb,

            // Stream cipher, not standard
            Aes128Ofb,
            Aes192Ofb,
            Aes256Ofb,
            Aes128Cfb1,
            Aes192Cfb1,
            Aes256Cfb1,
            Aes128Cfb8,
            Aes192Cfb8,
            Aes256Cfb8,

            // Stream cipher, deprecated
            Chacha20,
            BfCfb,
            Rc4Md5,
            Salsa20,

            // Not standard and not in acitve use
            DesCfb,
            IdeaCfb,
            Rc2Cfb,
            Cast5Cfb,
            Salsa20Ctr,
            Rc4,
            SeedCfb,
            Table
        }

        private readonly Dictionary<string, string> _encryptionTexts = new()
        {
            { "Chacha20IetfPoly1305", "chacha20-ietf-poly1305" },
            { "Aes128Gcm", "aes-128-gcm" },
            { "Aes192Gcm", "aes-192-gcm" },
            { "Aes256Gcm", "aes-256-gcm" },

            { "XChacha20IetfPoly1305", "xchacha20-ietf-poly1305" },

            { "Aes128Cfb", "aes-128-cfb" },
            { "Aes192Cfb", "aes-192-cfb" },
            { "Aes256Cfb", "aes-256-cfb" },
            { "Aes128Ctr", "aes-128-ctr" },
            { "Aes192Ctr", "aes-192-ctr" },
            { "Aes256Ctr", "aes-256-ctr" },
            { "Camellia128Cfb", "camellia-128-cfb" },
            { "Camellia192Cfb", "camellia-192-cfb" },
            { "Camellia256Cfb", "camellia-256-cfb" },
            { "Chacha20Ietf", "chacha20-ietf" },

            { "Aes256Cb", "aes-256-cfb" },

            { "Aes128Ofb", "aes-128-ofb" },
            { "Aes192Ofb", "aes-192-ofb" },
            { "Aes256Ofb", "aes-256-ofb" },
            { "Aes128Cfb1", "aes-128-cfb1" },
            { "Aes192Cfb1", "aes-192-cfb1" },
            { "Aes256Cfb1", "aes-256-cfb1" },
            { "Aes128Cfb8", "aes-128-cfb8" },
            { "Aes192Cfb8", "aes-192-cfb8" },
            { "Aes256Cfb8", "aes-256-cfb8" },

            { "Chacha20", "chacha20" },
            { "BfCfb", "bf-cfb" },
            { "Rc4Md5", "rc4-md5" },
            { "Salsa20", "salsa20" },

            { "DesCfb", "des-cfb" },
            { "IdeaCfb", "idea-cfb" },
            { "Rc2Cfb", "rc2-cfb" },
            { "Cast5Cfb", "cast5-cfb" },
            { "Salsa20Ctr", "salsa20-ctr" },
            { "Rc4", "rc4" },
            { "SeedCfb", "seed-cfb" },
            { "Table", "table" }
        };

        private readonly string _hostname, _password, _methodStr;
        private readonly Method _method;
        private readonly string? _parameter;
        private readonly int _port;
        private readonly string? _tag;

        private readonly Dictionary<string, string> _urlEncodeTable = new()
        {
            [" "] = "+",
            ["\0"] = "%00",
            ["\t"] = "%09",
            ["\n"] = "%0a",
            ["\r"] = "%0d",
            ["\""] = "%22",
            ["#"] = "%23",
            ["$"] = "%24",
            ["%"] = "%25",
            ["&"] = "%26",
            ["'"] = "%27",
            ["+"] = "%2b",
            [","] = "%2c",
            ["/"] = "%2f",
            [":"] = "%3a",
            [";"] = "%3b",
            ["<"] = "%3c",
            ["="] = "%3d",
            [">"] = "%3e",
            ["?"] = "%3f",
            ["@"] = "%40",
            ["["] = "%5b",
            ["\\"] = "%5c",
            ["]"] = "%5d",
            ["^"] = "%5e",
            ["`"] = "%60",
            ["{"] = "%7b",
            ["|"] = "%7c",
            ["}"] = "%7d",
            ["~"] = "%7e"
        };

        /// <summary>
        ///     Generates a ShadowSocks proxy config payload.
        /// </summary>
        /// <param name="hostname">Hostname of the ShadowSocks proxy</param>
        /// <param name="port">Port of the ShadowSocks proxy</param>
        /// <param name="password">Password of the SS proxy</param>
        /// <param name="method">Encryption type</param>
        /// <param name="tag">Optional tag line</param>
        public ShadowSocksConfig(string hostname, int port, string password, Method method, string? tag = null) :
            this(hostname, port, password, method, null, tag)
        {
        }

        public ShadowSocksConfig(string hostname, int port, string password, Method method, string plugin,
            string pluginOption, string? tag = null) :
            this(hostname, port, password, method, new Dictionary<string, string>
            {
                ["plugin"] = plugin + (
                    string.IsNullOrEmpty(pluginOption)
                        ? ""
                        : $";{pluginOption}"
                )
            }, tag)
        {
        }

        public ShadowSocksConfig(string hostname, int port, string password, Method method,
            Dictionary<string, string>? parameters, string? tag = null)
        {
            _hostname = Uri.CheckHostName(hostname) == UriHostNameType.IPv6
                ? $"[{hostname}]"
                : hostname;
            if (port is < 1 or > 65535)
            {
                throw new ShadowSocksConfigException("Value of 'port' must be within 0 and 65535.");
            }

            _port = port;
            _password = password;
            _method = method;
            _methodStr = _encryptionTexts[method.ToString()];
            _tag = tag;

            if (parameters is not null)
            {
                _parameter =
                    string.Join("&",
                        parameters.Select(
                            kv => $"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}"
                        ).ToArray());
            }
        }

        private string UrlEncode(string i)
        {
            return _urlEncodeTable.Aggregate(i, (current, kv) => current.Replace(kv.Key, kv.Value));
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_parameter))
            {
                var connectionString = $"{_methodStr}:{_password}@{_hostname}:{_port}";
                var connectionStringEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(connectionString));
                return $"ss://{connectionStringEncoded}{(!string.IsNullOrEmpty(_tag) ? $"#{_tag}" : string.Empty)}";
            }

            var authString = $"{_methodStr}:{_password}";
            var authStringEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return
                $"ss://{authStringEncoded}@{_hostname}:{_port}/?{_parameter}{(!string.IsNullOrEmpty(_tag) ? $"#{_tag}" : string.Empty)}";
        }

        public class ShadowSocksConfigException : Exception
        {
            public ShadowSocksConfigException()
            {
            }

            public ShadowSocksConfigException(string message)
                : base(message)
            {
            }

            public ShadowSocksConfigException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }

    public class MoneroTransaction : Payload
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

    public class SlovenianUpnQr : Payload
    {
        private readonly string _amount, _code;
        private readonly string? _deadLine;

        private readonly string _payerAddress,
            _payerName,
            _payerPlace,
            _purpose,
            _recipientAddress,
            _recipientIban,
            _recipientName,
            _recipientPlace,
            _recipientSiModel,
            _recipientSiReference;

        //Keep in mind, that the ECC level has to be set to "M", version to 15 and ECI to EciMode.Iso8859_2 when generating a SlovenianUpnQr!
        //SlovenianUpnQr specification: https://www.upn-Qr.si/uploads/files/NavodilaZaProgramerjeUPNQr.pdf

        public SlovenianUpnQr(string payerName, string payerAddress, string payerPlace, string recipientName,
            string recipientAddress, string recipientPlace, string recipientIban, string description, double amount,
            string recipientSiModel = "SI00", string recipientSiReference = "", string code = "OTHR") :
            this(payerName, payerAddress, payerPlace, recipientName, recipientAddress, recipientPlace, recipientIban,
                description, amount, null, recipientSiModel, recipientSiReference, code)
        {
        }

        public SlovenianUpnQr(string payerName, string payerAddress, string payerPlace, string recipientName,
            string recipientAddress, string recipientPlace, string recipientIban, string description, double amount,
            DateTime? deadline, string recipientSiModel = "SI99", string recipientSiReference = "",
            string code = "OTHR")
        {
            _payerName = LimitLength(payerName.Trim(), 33);
            _payerAddress = LimitLength(payerAddress.Trim(), 33);
            _payerPlace = LimitLength(payerPlace.Trim(), 33);
            _amount = FormatAmount(amount);
            _code = LimitLength(code.Trim().ToUpper(), 4);
            _purpose = LimitLength(description.Trim(), 42);
            _deadLine = deadline is null ? "" : deadline.Value.ToString("dd.MM.yyyy");
            _recipientIban = LimitLength(recipientIban.Trim(), 34);
            _recipientName = LimitLength(recipientName.Trim(), 33);
            _recipientAddress = LimitLength(recipientAddress.Trim(), 33);
            _recipientPlace = LimitLength(recipientPlace.Trim(), 33);
            _recipientSiModel = LimitLength(recipientSiModel.Trim().ToUpper(), 4);
            _recipientSiReference = LimitLength(recipientSiReference.Trim(), 22);
        }

        public override int Version => 15;
        public override QrCodeGenerator.ECCLevel EccLevel => QrCodeGenerator.ECCLevel.M;
        public override QrCodeGenerator.EciMode EciMode => QrCodeGenerator.EciMode.Iso8859_2;

        private static string LimitLength(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }


        private static string FormatAmount(double amount)
        {
            var amt = (int)Math.Round(amount * 100.0);
            return $"{amt:00000000000}";
        }

        private int CalculateChecksum()
        {
            var cs = 5 + _payerName.Length; //5 = UPNQr constant Length
            cs += _payerAddress.Length;
            cs += _payerPlace.Length;
            cs += _amount.Length;
            cs += _code.Length;
            cs += _purpose.Length;
            if (_deadLine is not null)
            {
                cs += _deadLine.Length;
            }

            cs += _recipientIban.Length;
            cs += _recipientName.Length;
            cs += _recipientAddress.Length;
            cs += _recipientPlace.Length;
            cs += _recipientSiModel.Length;
            cs += _recipientSiReference.Length;
            cs += 19;
            return cs;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("UPNQr");
            sb.Append('\n').Append('\n').Append('\n').Append('\n').Append('\n');
            sb.Append(_payerName).Append('\n');
            sb.Append(_payerAddress).Append('\n');
            sb.Append(_payerPlace).Append('\n');
            sb.Append(_amount).Append('\n').Append('\n').Append('\n');
            sb.Append(_code.ToUpper()).Append('\n');
            sb.Append(_purpose).Append('\n');
            sb.Append(_deadLine).Append('\n');
            sb.Append(_recipientIban.ToUpper()).Append('\n');
            sb.Append(_recipientSiModel).Append(_recipientSiReference).Append('\n');
            sb.Append(_recipientName).Append('\n');
            sb.Append(_recipientAddress).Append('\n');
            sb.Append(_recipientPlace).Append('\n');
            sb.Append($"{CalculateChecksum():000}").Append('\n');
            return sb.ToString();
        }
    }


    public class RussiaPaymentOrder : Payload
    {
        public enum CharacterSets
        {
            windows_1251 = 1, // Encoding.GetEncoding("windows-1251")
            utf_8 = 2, // Encoding.UTF8                          
            koi8_r = 3 // Encoding.GetEncoding("koi8-r")
        }

        /// <summary>
        ///     (List of values of the technical code of the payment)
        ///     <para>Перечень значений технического кода платежа</para>
        /// </summary>
        public enum TechCode
        {
            Мобильная_связь_стационарный_телефон = 01,
            Коммунальные_услуги_ЖКХAFN = 02,
            ГИБДД_налоги_пошлины_бюджетные_платежи = 03,
            Охранные_услуги = 04,
            Услуги_оказываемые_УФМС = 05,
            ПФР = 06,
            Погашение_кредитов = 07,
            Образовательные_учреждения = 08,
            Интернет_и_ТВ = 09,
            Электронные_деньги = 10,
            Отдых_и_путешествия = 11,
            Инвестиции_и_страхование = 12,
            Спорт_и_здоровье = 13,
            Благотворительные_и_общественные_организации = 14,
            Прочие_услуги = 15
        }
        // Specification of RussianPaymentOrder
        //https://docs.cntd.ru/document/1200110981
        //https://roskazna.gov.ru/upload/iblock/5fa/gost_r_56042_2014.pdf
        //https://sbQr.ru/standard/files/standart.pdf

        // Specification of data types described in the above standard
        // https://gitea.sergeybochkov.com/bochkov/emuik/src/commit/d18f3b550f6415ea4a4a5e6097eaab4661355c72/template/ed

        // Tool for Qr validation
        // https://www.sbQr.ru/validator/index.html

        //base
        private readonly CharacterSets _characterSet;
        private readonly MandatoryFields _mFields;
        private readonly OptionalFields _oFields;
        private string _separator = "|";

        private RussiaPaymentOrder()
        {
            _mFields = new MandatoryFields();
            _oFields = new OptionalFields();
        }

        /// <summary>
        ///     Generates a RussiaPaymentOrder payload
        /// </summary>
        /// <param name="name">Name of the payee (Наименование получателя платежа)</param>
        /// <param name="personalAcc">Beneficiary account number (Номер счета получателя платежа)</param>
        /// <param name="bankName">Name of the beneficiary's bank (Наименование банка получателя платежа)</param>
        /// <param name="bic">BIC (БИК)</param>
        /// <param name="correspAcc">Box number / account payee's bank (Номер кор./сч. банка получателя платежа)</param>
        /// <param name="optionalFields">An (optional) object of additional fields</param>
        /// <param name="characterSet">Type of encoding (default UTF-8)</param>
        public RussiaPaymentOrder(string name, string personalAcc, string bankName, string bic, string correspAcc,
            OptionalFields? optionalFields = null, CharacterSets characterSet = CharacterSets.utf_8) : this()
        {
            _characterSet = characterSet;
            _mFields.Name = ValidateInput(name, "Name", @"^.{1,160}$");
            _mFields.PersonalAcc = ValidateInput(personalAcc, "PersonalAcc", @"^[1-9]\d{4}[0-9ABCEHKMPTX]\d{14}$");
            _mFields.BankName = ValidateInput(bankName, "BankName", @"^.{1,45}$");
            _mFields.Bic = ValidateInput(bic, "BIC", @"^\d{9}$");
            _mFields.CorrespAcc = ValidateInput(correspAcc, "CorrespAcc", @"^[1-9]\d{4}[0-9ABCEHKMPTX]\d{14}$");

            if (optionalFields is not null)
            {
                _oFields = optionalFields;
            }
        }

        /// <summary>
        ///     Returns payload as string.
        /// </summary>
        /// <remarks>
        ///     ⚠ Attention: If CharacterSets was set to windows-1251 or koi8-r you should use ToBytes() instead of ToString()
        ///     and pass the bytes to CreateQrCode()!
        /// </remarks>
        /// <returns></returns>
        public override string ToString()
        {
            var cp = _characterSet.ToString().Replace("_", "-");
            var bytes = ToBytes();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(cp).GetString(bytes);
        }

        /// <summary>
        ///     Returns payload as byte[].
        /// </summary>
        /// <remarks>Should be used if CharacterSets equals windows-1251 or koi8-r</remarks>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            //Calculate the separator
            _separator = DetermineSeparator();

            //Create the payload string
            var ret = "ST0001" + (int)_characterSet + //(separator != "|" ? separator : "") + 
                      $"{_separator}Name={_mFields.Name}" +
                      $"{_separator}PersonalAcc={_mFields.PersonalAcc}" +
                      $"{_separator}BankName={_mFields.BankName}" +
                      $"{_separator}BIC={_mFields.Bic}" +
                      $"{_separator}CorrespAcc={_mFields.CorrespAcc}";

            //Add optional fields, if filled
            var optionalFieldsList = GetOptionalFieldsAsList();
            if (optionalFieldsList.Count > 0)
            {
                ret += $"|{string.Join("|", optionalFieldsList.ToArray())}";
            }

            ret += _separator;

            //Encode return string as byte[] with correct CharacterSet
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var cp = _characterSet.ToString().Replace("_", "-");
            var bytesOut = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(cp), Encoding.UTF8.GetBytes(ret));
            if (bytesOut.Length > 300)
            {
                throw new RussiaPaymentOrderException(
                    $"Data too long. Payload must not exceed 300 bytes, but actually is {bytesOut.Length} bytes long. Remove additional data fields or shorten strings/values.");
            }

            return bytesOut;
        }


        /// <summary>
        ///     Determines a valid separator
        /// </summary>
        /// <returns></returns>
        private string DetermineSeparator()
        {
            // See chapter 5.2.1 of Standard (https://sbQr.ru/standard/files/standart.pdf)

            var mandatoryValues = GetMandatoryFieldsAsList();
            var optionalValues = GetOptionalFieldsAsList();

            // Possible candidates for field separation
            var separatorCandidates = new[]
            {
                "|", "#", ";", ":", "^", "_", "~", "{", "}", "!", "#", "$", "%", "&", "(", ")", "*", "+", ",", "/", "@"
            };
            foreach (var sepCandidate in separatorCandidates)
            {
                if (!mandatoryValues.Any(x => x.Contains(sepCandidate)) &&
                    !optionalValues.Any(x => x.Contains(sepCandidate)))
                {
                    return sepCandidate;
                }
            }

            throw new RussiaPaymentOrderException("No valid separator found.");
        }

        /// <summary>
        ///     Takes all optional fields that are not null and returns their string representation
        /// </summary>
        /// <returns>A List of strings</returns>
        private List<string> GetOptionalFieldsAsList()
        {
            return _oFields.GetType().GetProperties()
                .Where(field => field.GetValue(_oFields, null) is not null)
                .Select(field =>
                {
                    var objValue = field.GetValue(_oFields, null);
                    var value = field.PropertyType == typeof(DateTime?)
                        ? ((DateTime)(objValue ?? throw new InvalidOperationException())).ToString("dd.MM.yyyy")
                        : objValue?.ToString();
                    return $"{field.Name}={value}";
                })
                .ToList();
        }


        /// <summary>
        ///     Takes all mandatory fields that are not null and returns their string representation
        /// </summary>
        /// <returns>A List of strings</returns>
        private List<string> GetMandatoryFieldsAsList()
        {
            return _mFields.GetType().GetFields()
                .Where(field => field.GetValue(_mFields) is not null)
                .Select(field =>
                {
                    var objValue = field.GetValue(_mFields);
                    var value = field.FieldType == typeof(DateTime?)
                        ? ((DateTime)(objValue ?? throw new InvalidOperationException())).ToString("dd.MM.yyyy")
                        : objValue?.ToString();
                    return $"{field.Name}={value}";
                })
                .ToList();
        }

        /// <summary>
        ///     Validates a string against a given Regex pattern. Returns input if it matches the Regex expression (=valid) or
        ///     throws Exception in case there's a mismatch
        /// </summary>
        /// <param name="input">String to be validated</param>
        /// <param name="fieldname">Name/descriptor of the string to be validated</param>
        /// <param name="pattern">A regex pattern to be used for validation</param>
        /// <param name="errorText">An optional error text. If null, a standard error text is generated</param>
        /// <returns>Input value (in case it is valid)</returns>
        private static string ValidateInput(string input, string fieldname, string pattern, string? errorText = null)
        {
            return ValidateInput(input, fieldname, new[] { pattern }, errorText);
        }

        /// <summary>
        ///     Validates a string against one or more given Regex patterns. Returns input if it matches all regex expressions
        ///     (=valid) or throws Exception in case there's a mismatch
        /// </summary>
        /// <param name="input">String to be validated</param>
        /// <param name="fieldname">Name/descriptor of the string to be validated</param>
        /// <param name="patterns">An array of regex patterns to be used for validation</param>
        /// <param name="errorText">An optional error text. If null, a standard error text is generated</param>
        /// <returns>Input value (in case it is valid)</returns>
        private static string ValidateInput(string input, string fieldname, string[] patterns, string? errorText = null)
        {
            if (input is null)
            {
                throw new RussiaPaymentOrderException($"The input for '{fieldname}' must not be null.");
            }

            foreach (var pattern in patterns)
            {
                if (!Regex.IsMatch(input, pattern))
                {
                    throw new RussiaPaymentOrderException(errorText ??
                                                          $"The input for '{fieldname}' ({input}) doesn't match the pattern {pattern}");
                }
            }

            return input;
        }

        private class MandatoryFields
        {
            public string? BankName;
            public string? Bic;
            public string? CorrespAcc;
            public string? Name;
            public string? PersonalAcc;
        }

        public class OptionalFields
        {
            private string? _cbc;
            private string? _docNo;
            private string? _drawerStatus;
            private string? _kpp;
            private string? _oktmo;
            private string? _payeeInn;
            private string? _payerInn;
            private string? _paytReason;
            private string? _purpose;
            private string? _sum;
            private string? _taxPaytKind;
            private string? _taxPeriod;

            /// <summary>
            ///     Payment amount, in kopecks (FTI’s Amount.)
            ///     <para>Сумма платежа, в копейках</para>
            /// </summary>
            public string? Sum
            {
                get => _sum;
                set
                {
                    if (value is not null)
                    {
                        _sum = ValidateInput(value, "Sum", @"^\d{1,18}$");
                    }
                }
            }

            /// <summary>
            ///     Payment name (purpose)
            ///     <para>Наименование платежа (назначение)</para>
            /// </summary>
            public string? Purpose
            {
                get => _purpose;
                set
                {
                    if (value is not null)
                    {
                        _purpose = ValidateInput(value, "Purpose", @"^.{1,160}$");
                    }
                }
            }

            /// <summary>
            ///     Payee's INN (Resident Tax Identification Number; Text, up to 12 characters.)
            ///     <para>ИНН получателя платежа</para>
            /// </summary>
            public string? PayeeINN
            {
                get => _payeeInn;
                set
                {
                    if (value is not null)
                    {
                        _payeeInn = ValidateInput(value, "PayeeINN", @"^.{1,12}$");
                    }
                }
            }

            /// <summary>
            ///     Payer's INN (Resident Tax Identification Number; Text, up to 12 characters.)
            ///     <para>ИНН плательщика</para>
            /// </summary>
            public string? PayerINN
            {
                get => _payerInn;
                set
                {
                    if (value is not null)
                    {
                        _payerInn = ValidateInput(value, "PayerINN", @"^.{1,12}$");
                    }
                }
            }

            /// <summary>
            ///     Status compiler payment document
            ///     <para>Статус составителя платежного документа</para>
            /// </summary>
            public string? DrawerStatus
            {
                get => _drawerStatus;
                set
                {
                    if (value is not null)
                    {
                        _drawerStatus = ValidateInput(value, "DrawerStatus", @"^.{1,2}$");
                    }
                }
            }

            /// <summary>
            ///     KPP of the payee (Tax Registration Code; Text, up to 9 characters.)
            ///     <para>КПП получателя платежа</para>
            /// </summary>
            public string? KPP
            {
                get => _kpp;
                set
                {
                    if (value is not null)
                    {
                        _kpp = ValidateInput(value, "KPP", @"^.{1,9}$");
                    }
                }
            }

            /// <summary>
            ///     CBC
            ///     <para>КБК</para>
            /// </summary>
            public string? CBC
            {
                get => _cbc;
                set
                {
                    if (value is not null)
                    {
                        _cbc = ValidateInput(value, "CBC", @"^.{1,20}$");
                    }
                }
            }

            /// <summary>
            ///     All-Russian classifier territories of municipal formations
            ///     <para>Общероссийский классификатор территорий муниципальных образований</para>
            /// </summary>
            public string? OKTMO
            {
                get => _oktmo;
                set
                {
                    if (value is not null)
                    {
                        _oktmo = ValidateInput(value, "OKTMO", @"^.{1,11}$");
                    }
                }
            }

            /// <summary>
            ///     Basis of tax payment
            ///     <para>Основание налогового платежа</para>
            /// </summary>
            public string? PaytReason
            {
                get => _paytReason;
                set
                {
                    if (value is not null)
                    {
                        _paytReason = ValidateInput(value, "PaytReason", @"^.{1,2}$");
                    }
                }
            }

            /// <summary>
            ///     Taxable period
            ///     <para>Налоговый период</para>
            /// </summary>
            public string? TaxPeriod
            {
                get => _taxPeriod;
                set
                {
                    if (value is not null)
                    {
                        _taxPeriod = ValidateInput(value, "ТaxPeriod", @"^.{1,10}$");
                    }
                }
            }

            /// <summary>
            ///     Document number
            ///     <para>Номер документа</para>
            /// </summary>
            public string? DocNo
            {
                get => _docNo;
                set
                {
                    if (value is not null)
                    {
                        _docNo = ValidateInput(value, "DocNo", @"^.{1,15}$");
                    }
                }
            }

            /// <summary>
            ///     Document date
            ///     <para>Дата документа</para>
            /// </summary>
            public DateTime? DocDate { get; set; }

            /// <summary>
            ///     Payment type
            ///     <para>Тип платежа</para>
            /// </summary>
            public string? TaxPaytKind
            {
                get => _taxPaytKind;
                set
                {
                    if (value is not null)
                    {
                        _taxPaytKind = ValidateInput(value, "TaxPaytKind", @"^.{1,2}$");
                    }
                }
            }

            /**************************************************************************
             * The following fiels are no further specified in the standard
             * document (https://sbQr.ru/standard/files/standart.pdf) thus there
             * is no addition input validation implemented.
             * **************************************************************************/

            /// <summary>
            ///     Payer's surname
            ///     <para>Фамилия плательщика</para>
            /// </summary>
            public string? LastName { get; set; }

            /// <summary>
            ///     Payer's name
            ///     <para>Имя плательщика</para>
            /// </summary>
            public string? FirstName { get; set; }

            /// <summary>
            ///     Payer's patronymic
            ///     <para>Отчество плательщика</para>
            /// </summary>
            public string? MiddleName { get; set; }

            /// <summary>
            ///     Payer's address
            ///     <para>Адрес плательщика</para>
            /// </summary>
            public string? PayerAddress { get; set; }

            /// <summary>
            ///     Personal account of a budget recipient
            ///     <para>Лицевой счет бюджетного получателя</para>
            /// </summary>
            public string? PersonalAccount { get; set; }

            /// <summary>
            ///     Payment document index
            ///     <para>Индекс платежного документа</para>
            /// </summary>
            public string? DocIdx { get; set; }

            /// <summary>
            ///     Personal account number in the personalized accounting system in the Pension Fund of the Russian Federation - SNILS
            ///     <para>№ лицевого счета в системе персонифицированного учета в ПФР - СНИЛС</para>
            /// </summary>
            public string? PensAcc { get; set; }

            /// <summary>
            ///     Number of contract
            ///     <para>Номер договора</para>
            /// </summary>
            public string? Contract { get; set; }

            /// <summary>
            ///     Personal account number of the payer in the organization (in the accounting system of the PU)
            ///     <para>Номер лицевого счета плательщика в организации (в системе учета ПУ)</para>
            /// </summary>
            public string? PersAcc { get; set; }

            /// <summary>
            ///     Apartment number
            ///     <para>Номер квартиры</para>
            /// </summary>
            public string? Flat { get; set; }

            /// <summary>
            ///     Phone number
            ///     <para>Номер телефона</para>
            /// </summary>
            public string? Phone { get; set; }

            /// <summary>
            ///     DUL payer type
            ///     <para>Вид ДУЛ плательщика</para>
            /// </summary>
            public string? PayerIdType { get; set; }

            /// <summary>
            ///     DUL number of the payer
            ///     <para>Номер ДУЛ плательщика</para>
            /// </summary>
            public string? PayerIdNum { get; set; }

            /// <summary>
            ///     FULL NAME. child / student
            ///     <para>Ф.И.О. ребенка/учащегося</para>
            /// </summary>
            public string? ChildFio { get; set; }

            /// <summary>
            ///     Date of birth
            ///     <para>Дата рождения</para>
            /// </summary>
            public DateTime? BirthDate { get; set; }

            /// <summary>
            ///     Due date / Invoice date
            ///     <para>Срок платежа/дата выставления счета</para>
            /// </summary>
            public string? PaymTerm { get; set; }

            /// <summary>
            ///     Payment period
            ///     <para>Период оплаты</para>
            /// </summary>
            public string? PaymPeriod { get; set; }

            /// <summary>
            ///     Payment type
            ///     <para>Вид платежа</para>
            /// </summary>
            public string? Category { get; set; }

            /// <summary>
            ///     Service code / meter name
            ///     <para>Код услуги/название прибора учета</para>
            /// </summary>
            public string? ServiceName { get; set; }

            /// <summary>
            ///     Metering device number
            ///     <para>Номер прибора учета</para>
            /// </summary>
            public string? CounterId { get; set; }

            /// <summary>
            ///     Meter reading
            ///     <para>Показание прибора учета</para>
            /// </summary>
            public string? CounterVal { get; set; }

            /// <summary>
            ///     Notification, accrual, account number
            ///     <para>Номер извещения, начисления, счета</para>
            /// </summary>
            public string? QuittId { get; set; }

            /// <summary>
            ///     Date of notification / accrual / invoice / resolution (for traffic police)
            ///     <para>Дата извещения/начисления/счета/постановления (для ГИБДД)</para>
            /// </summary>
            public DateTime? QuittDate { get; set; }

            /// <summary>
            ///     Institution number (educational, medical)
            ///     <para>Номер учреждения (образовательного, медицинского)</para>
            /// </summary>
            public string? InstNum { get; set; }

            /// <summary>
            ///     Kindergarten / school class number
            ///     <para>Номер группы детсада/класса школы</para>
            /// </summary>
            public string? ClassNum { get; set; }

            /// <summary>
            ///     Full name of the teacher, specialist providing the service
            ///     <para>ФИО преподавателя, специалиста, оказывающего услугу</para>
            /// </summary>
            public string? SpecFio { get; set; }

            /// <summary>
            ///     Insurance / additional service amount / Penalty amount (in kopecks)
            ///     <para>Сумма страховки/дополнительной услуги/Сумма пени (в копейках)</para>
            /// </summary>
            public string? AddAmount { get; set; }

            /// <summary>
            ///     Resolution number (for traffic police)
            ///     <para>Номер постановления (для ГИБДД)</para>
            /// </summary>
            public string? RuleId { get; set; }

            /// <summary>
            ///     Enforcement Proceedings Number
            ///     <para>Номер исполнительного производства</para>
            /// </summary>
            public string? ExecId { get; set; }

            /// <summary>
            ///     Type of payment code (for example, for payments to Rosreestr)
            ///     <para>Код вида платежа (например, для платежей в адрес Росреестра)</para>
            /// </summary>
            public string? RegType { get; set; }

            /// <summary>
            ///     Unique accrual identifier
            ///     <para>Уникальный идентификатор начисления</para>
            /// </summary>
            public string? UIN { get; set; }

            /// <summary>
            ///     The technical code recommended by the service provider. Maybe used by the receiving organization to call the
            ///     appropriate processing IT system.
            ///     <para>
            ///         Технический код, рекомендуемый для заполнения поставщиком услуг. Может использоваться принимающей
            ///         организацией для вызова соответствующей обрабатывающей ИТ-системы.
            ///     </para>
            /// </summary>
            public TechCode? TechCode { get; set; }
        }

        public class RussiaPaymentOrderException : Exception
        {
            public RussiaPaymentOrderException(string message)
                : base(message)
            {
            }
        }
    }
}