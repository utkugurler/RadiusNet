using System.Net;
using System.Net.Sockets;
using RadiusNet.Packets;

namespace RadiusNet.Utils;

public class RadiusClient
{
    //private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    private int authPort = 1812;
    private int acctPort = 1813;
    private string hostName;
    private string sharedSecret;
    private UdpClient serverSocket;
    private int retryCount = 3;
    private int socketTimeout = 3000;
    private string authProtocol = AccessRequest.AUTH_PAP;

    public RadiusClient(string hostName, string sharedSecret)
    {
        SetHostName(hostName);
        SetSharedSecret(sharedSecret);
    }

    public RadiusClient(RadiusEndpoint client)
        : this(client.GetEndpointAddress().Address.ToString(), client.GetSharedSecret())
    {
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
        return response.PacketType == RadiusPacket.ACCESS_ACCEPT;
    }

    public RadiusPacket Authenticate(AccessRequest request)
    {
        //logger.Info("Send Access-Request packet: {0}", request);

        var response = Communicate(request, GetAuthPort());
        //logger.Info("Received packet: {0}", response);

        return response;
    }

    public RadiusPacket Account(AccountingRequest request)
    {
        //logger.Info("Send Accounting-Request packet: {0}", request);

        var response = Communicate(request, GetAcctPort());
        //logger.Info("Received packet: {0}", response);

        return response;
    }

    public void Close()
    {
        serverSocket?.Close();
    }

    public int GetAuthPort()
    {
        return authPort;
    }

    public void SetAuthPort(int authPort)
    {
        if (authPort < 1 || authPort > 65535)
            throw new ArgumentException("Bad port number");
        this.authPort = authPort;
    }

    public string GetHostName()
    {
        return hostName;
    }

    public void SetHostName(string hostName)
    {
        if (string.IsNullOrEmpty(hostName))
            throw new ArgumentException("Host name must not be empty");
        this.hostName = hostName;
    }

    public int GetRetryCount()
    {
        return retryCount;
    }

    public void SetRetryCount(int retryCount)
    {
        if (retryCount < 1)
            throw new ArgumentException("Retry count must be positive");
        this.retryCount = retryCount;
    }

    public string GetSharedSecret()
    {
        return sharedSecret;
    }

    public void SetSharedSecret(string sharedSecret)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            throw new ArgumentException("Shared secret must not be empty");
        this.sharedSecret = sharedSecret;
    }

    public int GetSocketTimeout()
    {
        return socketTimeout;
    }

    public void SetSocketTimeout(int socketTimeout)
    {
        if (socketTimeout < 1)
            throw new ArgumentException("Socket timeout must be positive");
        this.socketTimeout = socketTimeout;
        if (serverSocket != null)
            serverSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, socketTimeout);
    }

    public void SetAcctPort(int acctPort)
    {
        if (acctPort < 1 || acctPort > 65535)
            throw new ArgumentException("Bad port number");
        this.acctPort = acctPort;
    }

    public int GetAcctPort()
    {
        return acctPort;
    }

    public void SetAuthProtocol(string protocol)
    {
        this.authProtocol = protocol;
    }

    public RadiusPacket Communicate(RadiusPacket request, int port)
    {
        var remoteEndPoint = new IPEndPoint(Dns.GetHostAddresses(hostName)[0], port);
        var packetOut = MakeDatagramPacket(request, port);

        using (serverSocket = new UdpClient())
        {
            serverSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, socketTimeout);

            for (int i = 1; i <= retryCount; i++)
            {
                try
                {
                    serverSocket.Send(packetOut, packetOut.Length, remoteEndPoint);
                    var packetIn = serverSocket.Receive(ref remoteEndPoint);
                    return MakeRadiusPacket(packetIn, request);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    if (i == retryCount)
                    {
                        //logger.Error("Communication failure (timeout), no more retries");
                        throw;
                    }
                    //logger.Info("Communication failure, retry {0}", i);
                }
                catch (IOException ex)
                {
                    if (i == retryCount)
                    {
                        //logger.Error(ex, "Communication failure, no more retries");
                        throw;
                    }
                    //logger.Info("Communication failure, retry {0}", i);
                }
            }
        }

        return null;
    }

    public static RadiusPacket Communicate(RadiusEndpoint remoteServer, RadiusPacket request)
    {
        var rc = new RadiusClient(remoteServer);
        return rc.Communicate(request, remoteServer.GetEndpointAddress().Port);
    }

    protected UdpClient GetSocket()
    {
        if (serverSocket == null || !serverSocket.Client.Connected)
        {
            serverSocket = new UdpClient();
            serverSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, socketTimeout);
        }
        return serverSocket;
    }

    protected byte[] MakeDatagramPacket(RadiusPacket packet, int port)
    {
        using (var bos = new MemoryStream())
        {
            packet.EncodeRequestPacket(bos, GetSharedSecret());
            return bos.ToArray();
        }
    }

    protected RadiusPacket MakeRadiusPacket(byte[] packetData, RadiusPacket request)
    {
        using (var inStream = new MemoryStream(packetData))
        {
            return RadiusPacket.DecodeResponsePacket(inStream, GetSharedSecret(), request);
        }
    }
}