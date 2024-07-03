using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QrSharp;

public static class PayloadGenerator
{
    internal static bool IsValidIban(string iban)
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

    internal static bool IsValidQrIban(string iban)
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

    internal static bool IsValidBic(string bic)
    {
        return Regex.IsMatch(bic.Replace(" ", ""), @"^([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)$");
    }


    internal static string ConvertStringToEncoding(string message, string encoding)
    {
        var iso = Encoding.GetEncoding(encoding);
        var utf8 = Encoding.UTF8;
        var utfBytes = utf8.GetBytes(message);
        var isoBytes = Encoding.Convert(utf8, iso, utfBytes);
        return iso.GetString(isoBytes, 0, isoBytes.Length);
    }

    internal static string EscapeInput(string? inp, bool simple = false)
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


    internal static bool ChecksumMod10(string digits)
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

    internal static bool IsHexStyle(string inp)
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
}