namespace QrSharp.Extensions;

public class StringValueAttribute : Attribute
{
    /// <summary>
    ///     Init a StringValue Attribute
    /// </summary>
    /// <param name="value"></param>
    public StringValueAttribute(string value)
    {
        StringValue = value;
    }

    #region Properties

    /// <summary>
    ///     Holds the alue in an enum
    /// </summary>
    public string StringValue { get; protected set; }

    #endregion
}

public static class CustomExtensions
{
    /// <summary>
    ///     Will get the string value for a given enum's value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetStringValue(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attr = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
        return attr.Length > 0 ? attr[0].StringValue : null;
    }
}