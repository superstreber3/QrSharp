namespace QrSharp.HelperMethods;

internal static class StringHelper
{
    public static bool IsNullOrWhiteSpace(string? value)
    {
        if (value == null)
        {
            return true;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static string ReverseString(string str)
    {
        var chars = str.ToCharArray();
        var result = new char[chars.Length];
        for (int i = 0, j = str.Length - 1; i < str.Length; i++, j--)
        {
            result[i] = chars[j];
        }

        return new string(result);
    }

    public static bool IsAllDigit(string str)
    {
        foreach (var c in str)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}