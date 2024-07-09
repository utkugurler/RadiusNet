using System.Reflection;

namespace RadiusNet.Dictionary;

/// <summary>
/// The default dictionary is a singleton object containing
/// a dictionary in the memory that is filled on application
/// startup using the default dictionary file from the
/// classpath resource
/// <code>org.tinyradius.dictionary.default_dictionary</code>.
/// </summary>
public class DefaultDictionary : MemoryDictionary
{
    private static readonly string DICTIONARY_RESOURCE = "org.tinyradius.dictionary.default_dictionary";
    private static readonly DefaultDictionary instance;

    static DefaultDictionary()
    {
        try
        {
            instance = new DefaultDictionary();
            Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("tinyradius_dictionary");
            if (source == null)
            {
                source = Assembly.GetExecutingAssembly().GetManifestResourceStream(DICTIONARY_RESOURCE);
            }
            if (source != null)
            {
                DictionaryParser.ParseDictionary(source, instance);
            }
            else
            {
                throw new IOException("Dictionary resource not found.");
            }
        }
        catch (IOException e)
        {
            throw new Exception("Default dictionary unavailable", e);
        }
    }

    /// <summary>
    /// Returns the singleton instance of this object.
    /// </summary>
    /// <returns>DefaultDictionary instance</returns>
    public static IDictionary GetDefaultDictionary()
    {
        return instance;
    }

    /// <summary>
    /// Make constructor private so that a DefaultDictionary
    /// cannot be constructed by other classes.
    /// </summary>
    private DefaultDictionary()
    {
    }
}