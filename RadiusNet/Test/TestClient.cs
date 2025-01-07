using RadiusNet.Attribute;
using RadiusNet.Packets;
using RadiusNet.Utils;

namespace RadiusNet.Test;

public class TestClient
{
    public static void Main()
    {
        CoaRequest coaRequest = new CoaRequest();
        RadiusClient client = new RadiusClient("193.192.126.156", "ka05ur15");
        coaRequest.SetPacketType(RadiusPacket.DISCONNECT_REQUEST);
        coaRequest.AddAttribute("User-Name", "operasyon2test@turk.net");

        try
        {
            RadiusPacket response = client.Communicate(coaRequest, 1645);
            if (response != null && response.PacketType == RadiusPacket.DISCONNECT_ACK)
            {
                Console.WriteLine("Disconnect request acknowledged.");
            }
            else
            {
                Console.WriteLine("Failed to receive a valid response.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
