using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RadiusNet.Packets;
using RadiusNet.Utils;

/// <summary>
/// Represents a RADIUS client that communicates with a specified RADIUS server.
/// </summary>
public class RadiusClient
{
    private int authPort = 1812;
    private int acctPort = 1813;
    private string hostName;
    private string sharedSecret;
    private int retryCount = 3;
    private int socketTimeout = 3000;
    private string authProtocol = AccessRequest.AUTH_PAP;

    public RadiusClient(string hostName, string sharedSecret)
    {
        SetHostName(hostName);
        SetSharedSecret(sharedSecret);
    }

    public RadiusClient(RadiusEndpoint client)
    {
        SetHostName(client.EndpointAddress.Address.ToString());
        SetSharedSecret(client.SharedSecret);
    }

    public bool Authenticate(string userName, string password)
    {
        return Authenticate(userName, password, authProtocol);
    }

    public bool Authenticate(string userName, string password, string protocol)
    {
        var request = new AccessRequest(userName, password);
        request.SetAuthProtocol(protocol);
        var response = Authenticate(request);
        return response != null && response.PacketType == RadiusPacket.ACCESS_ACCEPT;
    }

    public RadiusPacket Authenticate(AccessRequest request)
    {
        Console.WriteLine($"send Access-Request packet: {request}");

        var response = Communicate(request, GetAuthPort());
        Console.WriteLine($"received packet: {response}");

        return response;
    }

    public RadiusPacket Account(AccountingRequest request)
    {
        Console.WriteLine($"send Accounting-Request packet: {request}");

        var response = Communicate(request, GetAcctPort());
        Console.WriteLine($"received packet: {response}");

        return response;
    }
    

    public int GetAuthPort() => authPort;

    public void SetAuthPort(int authPort)
    {
        if (authPort < 1 || authPort > 65535)
            throw new ArgumentException("Invalid port number.");
        this.authPort = authPort;
    }

    public string GetHostName() => hostName;

    public void SetHostName(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName) || !IPAddress.TryParse(hostName, out _))
            throw new ArgumentException("Invalid host name or IP address.");
        this.hostName = hostName;
    }

    public int GetRetryCount() => retryCount;

    public void SetRetryCount(int retryCount)
    {
        if (retryCount < 1)
            throw new ArgumentException("Retry count must be positive.");
        this.retryCount = retryCount;
    }

    public string GetSharedSecret() => sharedSecret;

    public void SetSharedSecret(string sharedSecret)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            throw new ArgumentException("Shared secret must not be empty.");
        this.sharedSecret = sharedSecret;
    }

    public int GetSocketTimeout() => socketTimeout;

    public void SetSocketTimeout(int socketTimeout)
    {
        if (socketTimeout < 1)
            throw new ArgumentException("Socket timeout must be positive.");
        this.socketTimeout = socketTimeout;
    }

    public void SetAcctPort(int acctPort)
    {
        if (acctPort < 1 || acctPort > 65535)
            throw new ArgumentException("Invalid port number.");
        this.acctPort = acctPort;
    }

    public int GetAcctPort() => acctPort;

    public void SetAuthProtocol(string protocol)
    {
        this.authProtocol = protocol;
    }

    public RadiusPacket Communicate(RadiusPacket request, int port)
    {
        byte[] packetIn = new byte[RadiusPacket.MAX_PACKET_LENGTH];
        byte[] packetOut = MakeDatagramPacket(request);

        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.ReceiveTimeout = GetSocketTimeout();
            var address = IPAddress.Parse(GetHostName());
            var endPoint = new IPEndPoint(address, port);

            for (int i = 1; i <= GetRetryCount(); i++)
            {
                try
                {
                    socket.SendTo(packetOut, endPoint);
                    Console.WriteLine(BitConverter.ToString(packetOut));
                    socket.Receive(packetIn);
                    return MakeRadiusPacket(packetIn, request);
                }
                catch (SocketException ex)
                {
                    if (i == GetRetryCount())
                    {
                        Console.WriteLine("Communication failure (timeout), no more retries");
                        throw;
                    }
                    Console.WriteLine($"Communication failure, retry {i}");
                }
            }
        }

        return null;
    }

    protected byte[] MakeDatagramPacket(RadiusPacket packet)
    {
        using (var memoryStream = new MemoryStream())
        {
            packet.EncodeRequestPacket(memoryStream, GetSharedSecret());
            byte[] data = memoryStream.ToArray();

            var endPoint = new IPEndPoint(IPAddress.Parse(GetHostName()), 1645);
            return data;
        }
    }

    protected RadiusPacket MakeRadiusPacket(byte[] packet, RadiusPacket request)
    {
        using (var inStream = new MemoryStream(packet))
        {
            return RadiusPacket.DecodeResponsePacket(inStream, GetSharedSecret(), request);
        }
    }
}
