using System.Security.Cryptography;
using System.Text;
using RadiusNet.Utils;

namespace RadiusNet.Packets;

/// <summary>
/// CoA packet. Thanks to Michael Krastev.
/// </summary>
public class CoaRequest : RadiusPacket
{
    public CoaRequest() : this(COA_REQUEST)
    {
    }

    public CoaRequest(int type) : base(type, GetNextPacketIdentifier())
    {
    }

    /// <summary>
    /// Updates the request authenticator.
    /// </summary>
    /// <param name="sharedSecret">Shared secret</param>
    /// <param name="packetLength">Packet length</param>
    /// <param name="attributes">Attributes</param>
    /// <returns>Updated authenticator</returns>
    protected override byte[] UpdateRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
    {
        byte[] authenticator = new byte[16];
        
        using (var md5 = MD5.Create())
        using (var combinedStream = new MemoryStream())
        using (var writer = new BinaryWriter(combinedStream))
        {
            // Write packet header components
            writer.Write((byte)PacketType);
            writer.Write((byte)PacketIdentifier);
            writer.Write((short)packetLength);
            writer.Write(authenticator);  // 16 bytes of zeros
            writer.Write(attributes);
            writer.Write(Encoding.UTF8.GetBytes(sharedSecret));
            
            // Calculate MD5 hash
            return md5.ComputeHash(combinedStream.ToArray());
        }
    }

}
