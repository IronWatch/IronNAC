using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using Radius;
using Radius.RadiusAttributes;
using RadiusPlayground;

UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1812));
IPEndPoint remoteEndpoint = new(IPAddress.Any, 0);

byte[] secret = Encoding.ASCII.GetBytes("thesecret");
byte lastSeenIdentifier = 0;

RadiusAttributeParser parser = new();
parser.AddDefault();
Console.WriteLine($"Discovered {parser.Types.Count} parseable IRadiusAttribute Types\n");

List<SessionRecord> sessions = new();
List<ClientRecord> clients = new();

IRadiusAttribute[] vlanAssignmentAttributes = [
    new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN),
    new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802)
];

try
{
    while (true)
    {
        byte[] packet = udpClient.Receive(ref remoteEndpoint);
        RadiusPacket? incoming = null;

        try
        {
            incoming = RadiusPacket.FromBytes(packet, parser);
        }
        catch (RadiusException radEx)
        {
            Console.WriteLine(radEx.Message);
            continue;
        }

        Console.WriteLine($"Received {incoming.Code}");
        lastSeenIdentifier = incoming.Identifier;
        string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
        ClientRecord? client = clients.Where(x => x.ClientMAC == mac).FirstOrDefault();
        SessionRecord? session = sessions.Where(x => x.ClientMAC == mac).FirstOrDefault();

        RadiusPacket response;

        switch (incoming.Code)
        {
            case RadiusCode.ACCESS_REQUEST:

                if (mac is null) break;
                
                Console.Write($"Access Request {mac} ");

                if (client is not null &&
                    client.Blocked)
                {
                    // Blocked Client
                    response = RadiusPacket.Create(
                        RadiusCode.ACCESS_REJECT,
                        incoming.Identifier)
                        .AddMessageAuthenticator(secret)
                        .AddResponseAuthenticator(secret, incoming.Authenticator);
                    Console.WriteLine("BLOCKED CLIENT");
                }
                else if (client is not null &&
                    client.Guest)
                {
                    // Guest
                    response = RadiusPacket.Create(
                        RadiusCode.ACCESS_ACCEPT,
                        incoming.Identifier)
                        .AddAttributes(vlanAssignmentAttributes)
                        .AddAttribute(new TunnelPrivateGroupIdAttribute(0, "254"))
                        .AddMessageAuthenticator(secret)
                        .AddResponseAuthenticator(secret, incoming.Authenticator);
                    Console.WriteLine("GUEST");
                }
                else
                {
                    // Registration
                    response = RadiusPacket.Create(
                        RadiusCode.ACCESS_ACCEPT,
                        incoming.Identifier)
                        .AddAttributes(vlanAssignmentAttributes)
                        .AddAttribute(new TunnelPrivateGroupIdAttribute(0, "255"))
                        .AddMessageAuthenticator(secret)
                        .AddResponseAuthenticator(secret, incoming.Authenticator);
                    Console.WriteLine("REGISTRATION");

                    client = new() { ClientMAC = mac };
                    clients.Add(client);
                }

                udpClient.Send(response.ToBytes(), remoteEndpoint);
                
                break;

            case RadiusCode.ACCOUNTING_REQUEST:
                
                // We got the message
                response = RadiusPacket.Create(
                    RadiusCode.ACCOUNTING_RESPONSE,
                    incoming.Identifier)
                    .AddMessageAuthenticator(secret)
                    .AddResponseAuthenticator(secret, incoming.Authenticator);
                udpClient.Send(response.ToBytes(), remoteEndpoint);

                if (mac is null) break;

                AccountingStatusTypeAttribute? statusType = incoming.GetAttribute<AccountingStatusTypeAttribute>();
                if (statusType is null) break;

                switch (statusType.StatusType)
                {
                    case AccountingStatusTypeAttribute.StatusTypes.START:

                        if (mac is null) break;

                        if (session is null)
                        {
                            session = new();
                            Console.WriteLine($"New Session {mac}");
                        }
                        else
                        {
                            Console.WriteLine($"Updated Session {mac}");
                        }
                        
                        session.ClientMAC = mac;
                        session.UserName = incoming.GetAttribute<UserNameAttribute>();
                        session.NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>();
                        session.NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>();
                        session.CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>();
                        session.CalledStationId = incoming.GetAttribute<CalledStationIdAttribute>();
                        session.AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>();


                        // disconnect testing
                        /*
                        if (client is null) break;
                        if (session.NasIpAddress is null) break;

                        Console.Write($"VLAN Change Testing {mac} ");
                        if (!client.Blocked && !client.Guest)
                        {
                            client.Guest = true;
                            Console.WriteLine("from REGISTRATION to GUEST");
                        }
                        else if (!client.Blocked && client.Guest)
                        {
                            client.Blocked = true;
                            client.Guest = false;
                            Console.WriteLine("from GUEST to BLOCKED");
                        }                        

                        Console.WriteLine("Sleeping 5 seconds before Disconnect");
                        Thread.Sleep(5000);
                        Console.WriteLine("Attempting Disconnect");

                        RadiusPacket disconnect = RadiusPacket.Create(
                            RadiusCode.DISCONNECT_REQUEST,
                            ++lastSeenIdentifier,
                            null)
                            .AddAttribute(session.CallingStationId)
                            .AddAttribute(session.NasIpAddress)
                            .AddAttribute(session.NasIdentifier)
                            .AddAttribute(session.AccountingSessionId);

                        disconnect.ReplaceAuthenticator(disconnect.CalculateAuthenticator(secret));

                        udpClient.Send(disconnect.ToBytes(), new IPEndPoint(session.NasIpAddress.Address, 3799));
                        */
                        break;

                    case AccountingStatusTypeAttribute.StatusTypes.STOP:
                        sessions = sessions
                            .Where(x => x.ClientMAC != mac)
                            .ToList();
                        Console.WriteLine($"Removed Session {mac}");

                        break;
                    default:
                        break;
                }

                break;

            default:
                break;
        }        
    }
}
catch (SocketException sockEx)
{
    Console.WriteLine(sockEx.Message);
}
finally
{
    udpClient.Close();
}
