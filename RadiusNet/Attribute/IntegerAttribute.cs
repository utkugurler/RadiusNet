using RadiusNet.Dictionary;
using RadiusNet.Utils;

namespace RadiusNet.Attribute;

/// <summary>
/// This class represents a Radius attribute which only
/// contains a 32 bit integer.
/// </summary>
public class IntegerAttribute : RadiusAttribute
{
    /// <summary>
    /// Constructs an empty integer attribute.
    /// </summary>
    public IntegerAttribute() : base()
    {
    }

    /// <summary>
    /// Constructs an integer attribute with the given value.
    /// </summary>
    /// <param name="type">Attribute type</param>
    /// <param name="value">Attribute value</param>
    public IntegerAttribute(int type, int value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    /// <summary>
    /// Returns the string value of this attribute.
    /// </summary>
    /// <returns>A string</returns>
    public int GetAttributeValueInt()
    {
        byte[] data = GetAttributeData();
        return ((data[0] & 0x0ff) << 24) | ((data[1] & 0x0ff) << 16) |
               ((data[2] & 0x0ff) << 8) | (data[3] & 0x0ff);
    }

    /// <summary>
    /// Returns the value of this attribute as a string.
    /// Tries to resolve enumerations.
    /// </summary>
    /// <returns>Value as a string</returns>
    public override string GetAttributeValue()
    {
        int value = GetAttributeValueInt();
        AttributeType at = GetAttributeTypeObject();
        if (at != null)
        {
            string name = at.GetEnumeration(value);
            if (name != null)
                return name;
        }
        // Radius uses only unsigned values....
        return ((long)value & 0xffffffffL).ToString();
    }

    /// <summary>
    /// Sets the value of this attribute.
    /// </summary>
    /// <param name="value">Integer value</param>
    public void SetAttributeValue(int value)
    {
        byte[] data = new byte[4];
        data[0] = (byte)(value >> 24 & 0x0ff);
        data[1] = (byte)(value >> 16 & 0x0ff);
        data[2] = (byte)(value >> 8 & 0x0ff);
        data[3] = (byte)(value & 0x0ff);
        SetAttributeData(data);
    }

    /// <summary>
    /// Sets the value of this attribute.
    /// </summary>
    /// <param name="value">Value as a string</param>
    /// <exception cref="FormatException">If value is not a number and constant cannot be resolved</exception>
    public override void SetAttributeValue(string value)
    {
        AttributeType at = GetAttributeTypeObject();
        if (at != null)
        {
            int? val = at.GetEnumeration(value);
            if (val != null)
            {
                SetAttributeValue(val.Value);
                return;
            }
        }

        // Radius uses only unsigned integers; the parser should consider Long to parse high bit correctly...
        SetAttributeValue((int)long.Parse(value));
    }

    /// <summary>
    /// Check attribute length.
    /// </summary>
    /// <param name="data">Data buffer</param>
    /// <param name="offset">The offset to read</param>
    /// <param name="length">The amount of data to read</param>
    /// <exception cref="RadiusException">If length is less than 6</exception>
    public override void ReadAttribute(byte[] data, int offset, int length)
    {
        if (length != 6)
            throw new RadiusException("integer attribute: expected 4 bytes data");
        base.ReadAttribute(data, offset, length);
    }
}