using System.Security.Cryptography;
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
    protected byte[] UpdateRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
    {
        byte[] authenticator = new byte[16];
        Array.Clear(authenticator, 0, authenticator.Length);

        using (MD5 md5 = MD5.Create())
        {
            md5.Initialize();
            md5.TransformBlock(new byte[] { (byte)PacketType }, 0, 1, null, 0);
            md5.TransformBlock(new byte[] { (byte)PacketIdentifier }, 0, 1, null, 0);
            md5.TransformBlock(new byte[] { (byte)(packetLength >> 8) }, 0, 1, null, 0);
            md5.TransformBlock(new byte[] { (byte)(packetLength & 0xff) }, 0, 1, null, 0);
            md5.TransformBlock(authenticator, 0, authenticator.Length, null, 0);
            md5.TransformBlock(attributes, 0, attributes.Length, null, 0);
            md5.TransformFinalBlock(RadiusUtil.GetUtf8Bytes(sharedSecret), 0, RadiusUtil.GetUtf8Bytes(sharedSecret).Length);

            return md5.Hash;
        }
    }
}