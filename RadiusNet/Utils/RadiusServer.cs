using System.Net;
using System.Net.Sockets;
using RadiusNet.Packets;

namespace RadiusNet.Utils;

public abstract class RadiusServer
{
    // private readonly ILogger<RadiusServer> logger;
    private IPEndPoint listenAddress = null;
    private int authPort = 1812;
    private int acctPort = 1813;
    private UdpClient authSocket = null;
    private UdpClient acctSocket = null;
    private int socketTimeout = 3000;
    private Dictionary<string, long> receivedPackets = new Dictionary<string, long>();
    private long lastClean;
    private long duplicateInterval = 30000; // 30 s
    private bool closing = false;
    protected TaskScheduler taskScheduler = null;

    /*public RadiusServer(ILogger<RadiusServer> logger)
    {
        this.logger = logger;
    }*/

    public abstract string GetSharedSecret(IPEndPoint client);

    public virtual string GetSharedSecret(IPEndPoint client, RadiusPacket packet)
    {
        return GetSharedSecret(client);
    }

    public abstract string GetUserPassword(string userName);

    public virtual RadiusPacket AccessRequestReceived(AccessRequest accessRequest, IPEndPoint client)
    {
        string plaintext = GetUserPassword(accessRequest.GetUserName());
        int type = RadiusPacket.ACCESS_REJECT;
        if (plaintext != null && accessRequest.VerifyPassword(plaintext))
        {
            type = RadiusPacket.ACCESS_ACCEPT;
        }

        RadiusPacket answer = new RadiusPacket(type, accessRequest.PacketIdentifier);
        CopyProxyState(accessRequest, answer);
        return answer;
    }

    public virtual RadiusPacket AccountingRequestReceived(AccountingRequest accountingRequest, IPEndPoint client)
    {
        RadiusPacket answer = new RadiusPacket(RadiusPacket.ACCOUNTING_RESPONSE, accountingRequest.PacketIdentifier);
        CopyProxyState(accountingRequest, answer);
        return answer;
    }

    public void Start(bool listenAuth, bool listenAcct)
    {
        if (listenAuth)
        {
            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "Radius Auth Listener";
                try
                {
                    //logger.LogInformation("Starting RadiusAuthListener on port " + GetAuthPort());
                    ListenAuth();
                    //logger.LogInformation("RadiusAuthListener is being terminated");
                }
                catch (Exception e)
                {
                    //logger.LogError(e, "Auth thread stopped by exception");
                }
                finally
                {
                    authSocket.Close();
                    //logger.LogDebug("Auth socket closed");
                }
            });
        }

        if (listenAcct)
        {
            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "Radius Acct Listener";
                try
                {
                    //logger.LogInformation("Starting RadiusAcctListener on port " + GetAcctPort());
                    ListenAcct();
                    //logger.LogInformation("RadiusAcctListener is being terminated");
                }
                catch (Exception e)
                {
                    //logger.LogError(e, "Acct thread stopped by exception");
                }
                finally
                {
                    acctSocket.Close();
                    //logger.LogDebug("Acct socket closed");
                }
            });
        }
    }

    public void Stop()
    {
        //logger.LogInformation("Stopping Radius server");
        closing = true;
        authSocket?.Close();
        acctSocket?.Close();
    }

    public int GetAuthPort()
    {
        return authPort;
    }

    public void SetAuthPort(int authPort)
    {
        if (authPort < 1 || authPort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(authPort), "Bad port number");
        }
        this.authPort = authPort;
        this.authSocket = null;
    }

    public int GetSocketTimeout()
    {
        return socketTimeout;
    }

    public void SetSocketTimeout(int socketTimeout)
    {
        if (socketTimeout < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(socketTimeout), "Socket timeout must be positive");
        }
        this.socketTimeout = socketTimeout;
        if (authSocket != null)
        {
            authSocket.Client.ReceiveTimeout = socketTimeout;
        }
        if (acctSocket != null)
        {
            acctSocket.Client.ReceiveTimeout = socketTimeout;
        }
    }

    public void SetAcctPort(int acctPort)
    {
        if (acctPort < 1 || acctPort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(acctPort), "Bad port number");
        }
        this.acctPort = acctPort;
        this.acctSocket = null;
    }

    public int GetAcctPort()
    {
        return acctPort;
    }

    public long GetDuplicateInterval()
    {
        return duplicateInterval;
    }

    public void SetDuplicateInterval(long duplicateInterval)
    {
        if (duplicateInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duplicateInterval), "Duplicate interval must be positive");
        }
        this.duplicateInterval = duplicateInterval;
    }

    public Dictionary<string, long> GetReceivedPackets()
    {
        return receivedPackets;
    }

    public IPEndPoint GetListenAddress()
    {
        return listenAddress;
    }

    public void SetListenAddress(IPEndPoint listenAddress)
    {
        this.listenAddress = listenAddress;
    }

    protected void CopyProxyState(RadiusPacket request, RadiusPacket answer)
    {
        var proxyStateAttrs = request.GetAttributes(33);
        foreach (var proxyStateAttr in proxyStateAttrs)
        {
            answer.AddAttribute(proxyStateAttr);
        }
    }

    protected void ListenAuth()
    {
        Listen(GetAuthSocket());
    }

    protected void ListenAcct()
    {
        Listen(GetAcctSocket());
    }

    protected void Listen(UdpClient s)
    {
        while (true)
        {
            try
            {
                var packetIn = new byte[RadiusPacket.MAX_PACKET_LENGTH];
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                s.Client.ReceiveTimeout = socketTimeout;

                try
                {
                    //logger.LogTrace("About to call socket.Receive()");
                    packetIn = s.Receive(ref endpoint);
                    /*if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Receive buffer size = " + s.Client.ReceiveBufferSize);
                    }*/
                }
                catch (SocketException se)
                {
                    if (closing)
                    {
                        //logger.LogInformation("Got closing signal - end listen thread");
                        return;
                    }
                    //logger.LogError(se, "SocketException during s.Receive() -> retry");
                    continue;
                }

                if (taskScheduler == null)
                {
                    ProcessRequest(s, packetIn, endpoint);
                }
                else
                {
                    Task.Factory.StartNew(() => ProcessRequest(s, packetIn, endpoint), CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                }
            }
            catch (SocketException ste)
            {
                //logger.LogTrace("Normal socket timeout");
            }
            catch (IOException ioe)
            {
                //logger.LogError(ioe, "Communication error");
            }
        }
    }

    protected void ProcessRequest(UdpClient s, byte[] packetIn, IPEndPoint endpoint)
    {
        try
        {
            var remoteAddress = endpoint;
            var localAddress = s.Client.LocalEndPoint as IPEndPoint;
            var secret = GetSharedSecret(remoteAddress, MakeRadiusPacket(packetIn, "1234567890", RadiusPacket.RESERVED));
            if (secret == null)
            {
                /*if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Ignoring packet from unknown client " + remoteAddress + " received on local address " + localAddress);
                }*/
                return;
            }

            var request = MakeRadiusPacket(packetIn, secret, RadiusPacket.UNDEFINED);
            /*if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Received packet from " + remoteAddress + " on local address " + localAddress + ": " + request);
            }*/

            //logger.LogTrace("About to call RadiusServer.handlePacket()");
            var response = HandlePacket(localAddress, remoteAddress, request, secret);

            if (response != null)
            {
                /*if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Send response: " + response);
                }*/
                var packetOut = MakeDatagramPacket(response, secret, remoteAddress.Address, endpoint.Port, request);
                s.Send(packetOut, packetOut.Length, remoteAddress);
            }
            else
            {
                //logger.LogInformation("No response sent");
            }
        }
        catch (IOException ioe)
        {
            //logger.LogError(ioe, "Communication error");
        }
        catch (RadiusException re)
        {
            //logger.LogError(re, "Malformed Radius packet");
        }
    }

    protected RadiusPacket HandlePacket(IPEndPoint localAddress, IPEndPoint remoteAddress, RadiusPacket request, string sharedSecret)
    {
        RadiusPacket response = null;

        if (!IsPacketDuplicate(request, remoteAddress))
        {
            if (localAddress.Port == GetAuthPort())
            {
                if (request is AccessRequest accessRequest)
                {
                    response = AccessRequestReceived(accessRequest, remoteAddress);
                }
                else
                {
                    //logger.LogError("Unknown Radius packet type: " + request.PacketType);
                }
            }
            else if (localAddress.Port == GetAcctPort())
            {
                if (request is AccountingRequest accountingRequest)
                {
                    response = AccountingRequestReceived(accountingRequest, remoteAddress);
                }
                else
                {
                    //logger.LogError("Unknown Radius packet type: " + request.PacketType);
                }
            }
            else
            {
                //logger.LogError("Packet on unknown port: " + localAddress.Port);
            }
        }
        else
        {
            //logger.LogInformation("Ignore duplicate packet");
        }

        return response;
    }

    protected UdpClient GetAuthSocket()
    {
        if (authSocket == null)
        {
            authSocket = new UdpClient(GetAuthPort());
            authSocket.Client.ReceiveTimeout = socketTimeout;
        }
        return authSocket;
    }

    protected UdpClient GetAcctSocket()
    {
        if (acctSocket == null)
        {
            acctSocket = new UdpClient(GetAcctPort());
            acctSocket.Client.ReceiveTimeout = socketTimeout;
        }
        return acctSocket;
    }

    protected byte[] MakeDatagramPacket(RadiusPacket packet, string secret, IPAddress address, int port, RadiusPacket request)
    {
        using (var ms = new MemoryStream())
        {
            packet.EncodeResponsePacket(ms, secret, request);
            var data = ms.ToArray();
            return data;
        }
    }

    protected RadiusPacket MakeRadiusPacket(byte[] packet, string sharedSecret, int forceType)
    {
        using (var ms = new MemoryStream(packet))
        {
            return RadiusPacket.DecodeRequestPacket(ms, sharedSecret, forceType);
        }
    }

    protected bool IsPacketDuplicate(RadiusPacket packet, IPEndPoint address)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long intervalStart = now - GetDuplicateInterval();

        var uniqueKey = address.Address.ToString() + packet.PacketIdentifier + Convert.ToBase64String(packet.GetAuthenticator());

        lock (receivedPackets)
        {
            if (lastClean == 0 || lastClean < now - GetDuplicateInterval())
            {
                lastClean = now;
                foreach (var key in receivedPackets.Keys.ToList())
                {
                    if (receivedPackets[key] < intervalStart)
                    {
                        receivedPackets.Remove(key);
                    }
                }
            }

            if (!receivedPackets.ContainsKey(uniqueKey))
            {
                receivedPackets[uniqueKey] = now;
                return false;
            }
            else
            {
                return receivedPackets[uniqueKey] >= intervalStart;
            }
        }
    }
}