using System;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.Service.Tools.JsonMaskConverter;

public class MyDefaultJsonMaskConverter : JsonConverter
{
    const string DefaultMask = "***";

    /// <summary>
    /// If set, the property value will be set to this text.
    /// </summary>
    private string Text { get; set; } = DefaultMask;

    /// <summary>
    /// Shows the first x characters in the property value.
    /// </summary>
    private int ShowFirst { get; set; }

    /// <summary>
    /// Shows the last x characters in the property value.
    /// </summary>
    private int ShowLast { get; set; }

    /// <summary>
    /// If set, it will swap out each character with the default value. Note that this
    /// property will be ignored if <see cref="Text"/> has been set to custom value.
    /// </summary>
    private bool PreserveLength { get; set; }

    private bool IsDefaultMask()
    {
        return Text == DefaultMask;
    }

    public MyDefaultJsonMaskConverter()
    {
        ShowFirst = 0;
        ShowLast = 0;
        PreserveLength = false;
    }
    
    public MyDefaultJsonMaskConverter(int showFirst, int showLast, bool preserveLength)
    {
        ShowFirst = showFirst;
        ShowLast = showLast;
        PreserveLength = preserveLength;
    }
    
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue(FormatMaskedValue(value as string));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize(reader);
    }
    
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);    
    }

    private object FormatMaskedValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (ShowFirst == 0 && ShowLast == 0)
        {
            if (PreserveLength)
                return new string(Text[0], value.Length);

            return Text;
        }

        if (ShowFirst > 0 && ShowLast == 0)
        {
            var first = value.Substring(0, Math.Min(ShowFirst, value.Length));

            if (!PreserveLength || !IsDefaultMask())
                return first + Text;

            var mask = "";
            if (ShowFirst <= value.Length)
                mask = new(Text[0], value.Length - ShowFirst);

            return first + mask;

        }

        if (ShowFirst == 0 && ShowLast > 0)
        {
            var last = ShowLast > value.Length ? value : value.Substring(value.Length - ShowLast);

            if (!PreserveLength || !IsDefaultMask())
                return Text + last;

            var mask = "";
            if (ShowLast <= value.Length)
                mask = new(Text[0], value.Length - ShowLast);

            return mask + last;
        }

        if (ShowFirst > 0 && ShowLast > 0)
        {
            if (ShowFirst + ShowLast >= value.Length)
                return value;

            var first = value.Substring(0, ShowFirst);
            var last = value.Substring(value.Length - ShowLast);

            string? mask = null;
            if (PreserveLength && IsDefaultMask())
                mask = new string(Text[0], value.Length - ShowFirst - ShowLast);

            return first + (mask ?? Text) + last;
        }
        return value;
    }
}