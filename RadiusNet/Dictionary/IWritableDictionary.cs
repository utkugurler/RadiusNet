namespace RadiusNet.Dictionary;

public interface IWritableDictionary : IDictionary
{
    void AddVendor(int vendorId, string vendorName);
    void AddAttributeType(AttributeType attributeType);
}