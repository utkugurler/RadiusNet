using System.Net;

namespace RadiusNet.Utils;

/// <summary>
/// This class stores information about a Radius endpoint.
/// This includes the address of the remote endpoint and the shared secret
/// used for securing the communication.
/// </summary>
public class RadiusEndpoint
{
    private readonly IPEndPoint endpointAddress;
    private readonly string sharedSecret;

    /// <summary>
    /// Constructs a RadiusEndpoint object.
    /// </summary>
    /// <param name="remoteAddress">Remote address (ip and port number)</param>
    /// <param name="sharedSecret">Shared secret</param>
    public RadiusEndpoint(IPEndPoint remoteAddress, string sharedSecret)
    {
        this.endpointAddress = remoteAddress;
        this.sharedSecret = sharedSecret;
    }

    /// <summary>
    /// Returns the remote address.
    /// </summary>
    /// <returns>Remote address</returns>
    public IPEndPoint GetEndpointAddress()
    {
        return endpointAddress;
    }

    /// <summary>
    /// Returns the shared secret.
    /// </summary>
    /// <returns>Shared secret</returns>
    public string GetSharedSecret()
    {
        return sharedSecret;
    }
}