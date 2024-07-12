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
    private static readonly string DICTIONARY_RESOURCE = "Dictionary.DefaultDictionary";
    private static readonly DefaultDictionary instance;

    static DefaultDictionary()
    {
        try
        {
            var stream = new MemoryStream();
            instance = new DefaultDictionary();
            using (MemoryStream ms = new MemoryStream())
            using (FileStream file = new FileStream("DefaultDictionary", FileMode.Open, FileAccess.Read)) {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                ms.Write(bytes, 0, (int)file.Length);
                stream.Write(bytes, 0, (int)file.Length);
            }
            
            if (stream == null)
            {
                //source = Assembly.GetExecutingAssembly().GetManifestResourceStream(DICTIONARY_RESOURCE);
            }
            if (stream != null)
            {
                DictionaryParser.ParseDictionary(stream, instance);
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
    public DefaultDictionary()
    {
    }
}