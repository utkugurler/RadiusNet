namespace RadiusNet.Utils;

/// <summary>
/// An exception which occurs on Radius protocol errors like
/// invalid packets or malformed attributes.
/// </summary>
public class RadiusException : Exception
{
    public static readonly long SerialVersionUID = 2201204523946051388L;
    
    /// <summary>
    /// Constructs a RadiusException with a message.
    /// </summary>
    /// <param name="message">Error message</param>
    public RadiusException(string message) : base(message)
    {
    }

    
}