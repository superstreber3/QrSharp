﻿using System.Text;
using System.Text.RegularExpressions;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class RussiaPaymentOrder : QrSharp.PayloadGenerator.Payload
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