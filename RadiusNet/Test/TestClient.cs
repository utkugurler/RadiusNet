using RadiusNet.Packets;
using Xunit;
using Xunit.Abstractions;

namespace RadiusNet.Test;

public class TestClient
{
    private readonly ITestOutputHelper _output;
    
    public TestClient(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    private void CoaRequestTest()
    {
        CoaRequest coaRequest = new CoaRequest();
        RadiusClient client = new RadiusClient("193.192.126.156", "ka05ur15");
        coaRequest.PacketType = RadiusPacket.COA_REQUEST;
        coaRequest.AddAttribute("User-Name", "operasyon2test@turk.net");
        coaRequest.AddAttribute("NAS-IP-Address", "193.192.126.156");
        coaRequest.AddAttribute("Acct-Session-Id", "00000000F7000000000F3F72");
        coaRequest.AddAttribute("Cisco-AVPair", "subscriber:command=account-profile-status-query");

        RadiusPacket response = client.Communicate(coaRequest, 1645);

        _output.WriteLine($"{coaRequest}");
    }
    
    [Fact]
    private void AccessRequestTest()
    {
        RadiusClient client = new RadiusClient("10.2.134.250", "r1dk2y");
        
        AccessRequest request = new AccessRequest("gihcgnat2@turk.net", "123456789");
        request.SetAuthProtocol(AccessRequest.AUTH_PAP);
        request.AddAttribute("Framed-Protocol", "PPP");
        request.AddAttribute("Connect-Info", "4294967295/0");
        request.AddAttribute("NAS-Port-Type", "Virtual");
        request.AddAttribute("NAS-Identifier", "CISCO|CGNAT|0");
        request.AddAttribute("Service-Type", "Framed-User");
        request.AddAttribute("NAS-IP-Address", "193.192.126.156");

        RadiusPacket result = client.Authenticate(request);
        
        _output.WriteLine($"{result}");
    }
    
    [Fact]
    private void DisconnectRequestTest()
    {
        CoaRequest coaRequest = new CoaRequest();
        RadiusClient client = new RadiusClient("193.192.126.156", "ka05ur15");
        coaRequest.SetPacketType(RadiusPacket.DISCONNECT_REQUEST);
        coaRequest.AddAttribute("User-Name", "operasyon2test@turk.net");

        RadiusPacket response = client.Communicate(coaRequest, 1645);
        _output.WriteLine($"{response}");
    }
}
