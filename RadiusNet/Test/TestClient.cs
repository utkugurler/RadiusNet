using RadiusNet.Attribute;
using RadiusNet.Packets;
using RadiusNet.Utils;

namespace RadiusNet.Test;

public class TestClient
{
    //private static readonly ILogger<TestClient> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TestClient>();

    /// <summary>
    /// Radius command line client.
    ///
    /// Usage: TestClient <i>hostName sharedSecret userName password</i>
    /// </summary>
    /// <param name="args">arguments</param>
    public static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: TestClient hostName sharedSecret userName password");
            Environment.Exit(1);
        }

        string host = "";
        string shared = "";
        string user = "";
        string pass = "";

        RadiusClient rc = new RadiusClient(host, shared);

        // 1. Send Access-Request
        AccessRequest ar = new AccessRequest(user, pass);
        ar.SetAuthProtocol(AccessRequest.AUTH_PAP);
        //ar.SetAuthProtocol(AccessRequest.AUTH_PAP); // or AUTH_CHAP
        ar.AddAttribute("NAS-Identifier", "abcds");
        ar.AddAttribute("NAS-IP-Address", "192.168.0.100");
        ar.AddAttribute("Service-Type", "Login-User");
        ar.AddAttribute("WISPr-Redirection-URL", "abcd");
        ar.AddAttribute("WISPr-Location-ID", "abcd");

        Console.WriteLine("Packet before it is sent\n" + ar + "\n");
        RadiusPacket response = rc.Authenticate(ar);
        Console.WriteLine("Packet after it was sent\n" + ar + "\n");
        Console.WriteLine("Response\n" + response + "\n");

        // 2. Send Accounting-Request
        AccountingRequest acc = new AccountingRequest("mw", AccountingRequest.ACCT_STATUS_TYPE_START);
        acc.AddAttribute("Acct-Session-Id", "1234567890");
        acc.AddAttribute("NAS-Identifier", "abcd");
        acc.AddAttribute("NAS-Port", "0");

        Console.WriteLine(acc + "\n");
        response = rc.Account(acc);
        Console.WriteLine("Response: " + response);

        rc.Close();
    }
}