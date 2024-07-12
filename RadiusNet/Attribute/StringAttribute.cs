using System.Text;

namespace RadiusNet.Attribute;

public class StringAttribute : RadiusAttribute
{
    /// <summary>
    /// Constructs an empty string attribute.
    /// </summary>
    public StringAttribute() : base()
    {
    }

    /// <summary>
    /// Constructs a string attribute with the given value.
    /// </summary>
    /// <param name="type">Attribute type</param>
    /// <param name="value">Attribute value</param>
    public StringAttribute(int type, string value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    /// <summary>
    /// Returns the string value of this attribute.
    /// </summary>
    /// <returns>A string</returns>
    public override string GetAttributeValue()
    {
        return Encoding.UTF8.GetString(GetAttributeData());
    }

    /// <summary>
    /// Sets the string value of this attribute.
    /// </summary>
    /// <param name="value">String, not null</param>
    public override void SetAttributeValue(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "string value not set");

        SetAttributeData(Encoding.UTF8.GetBytes(value));
    }
}