namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class ContactData : QrSharp.PayloadGenerator.Payload
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
}