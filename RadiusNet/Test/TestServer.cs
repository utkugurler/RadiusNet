using System.Net;
using RadiusNet.Attribute;
using RadiusNet.Packets;
using RadiusNet.Utils;

namespace RadiusNet.Test;

public class TestServer
{
    public static async Task Main(string[] args)
    {
        RadiusServer server = new MyRadiusServer();
        
        if (args.Length >= 1)
            server.SetAuthPort(1);
        if (args.Length >= 2)
            server.SetAcctPort(2);

        server.Start(true, true);

        Console.WriteLine("Server started.");

        await Task.Delay(TimeSpan.FromMinutes(30));
        Console.WriteLine("Stop server");
        server.Stop();
    }
}

public class MyRadiusServer : RadiusServer
{
    public override string GetSharedSecret(IPEndPoint client)
    {
        if (client.Address.ToString() == "127.0.0.1")
        {
            return "testing123";
        }
        return null;
    }

    public override string GetUserPassword(string userName)
    {
        if (userName == "mw")
        {
            return "test";
        }
        return null;
    }

    public override RadiusPacket AccessRequestReceived(AccessRequest accessRequest, IPEndPoint client)
    {
        Console.WriteLine("Received Access-Request:\n" + accessRequest);
        RadiusPacket packet = base.AccessRequestReceived(accessRequest, client);
        if (packet == null)
        {
            Console.WriteLine("Ignore packet.");
        }
        else if (packet.PacketType == RadiusPacket.ACCESS_ACCEPT)
        {
            packet.AddAttribute("Reply-Message", "Welcome " + accessRequest.GetUserName() + "!");
        }
        else
        {
            Console.WriteLine("Answer:\n" + packet);
        }
        return packet;
    }
}