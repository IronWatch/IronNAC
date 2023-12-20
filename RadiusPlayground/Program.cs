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

List<ClientRecord> clients = new();

IRadiusAttribute[] vlanAssignmentAttributes = [
    new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN),
    new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802)
];

string vlanToAssign = "254";

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

        RadiusPacket response;

        switch (incoming.Code)
        {
            case RadiusCode.ACCESS_REQUEST:

                Console.Write($"Request {incoming.Identifier} for {mac} ");
                Console.WriteLine("ACCEPTED");

                response = RadiusPacket.Create(
                    RadiusCode.ACCESS_ACCEPT,
                    incoming.Identifier,
                    attributes: vlanAssignmentAttributes)
                    .AddAttribute(new TunnelPrivateGroupIdAttribute(0, vlanToAssign))
                    .AddMessageAuthenticator(secret)
                    .AddResponseAuthenticator(secret, incoming.Authenticator);
                udpClient.Send(response.ToBytes(), remoteEndpoint);

                clients = clients
                    .Where(x => x.ClientMAC != mac)
                    .ToList();

                clients.Add(new()
                {
                    ClientMAC = mac,
                    VLAN = vlanToAssign,
                    UserName = incoming.GetAttribute<UserNameAttribute>(),
                    NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>(),
                    NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>(),
                    CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>(),
                    CalledStationId = incoming.GetAttribute<CalledStationIdAttribute>(),
                    AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>(),
                });
                Console.WriteLine($"Added {mac}");

                break;

            case RadiusCode.ACCOUNTING_REQUEST:
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
                        ClientRecord? client = clients.Where(x => x.ClientMAC == mac).FirstOrDefault();
                        if (client is null) break;

                        client.UserName = incoming.GetAttribute<UserNameAttribute>();
                        client.NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>();
                        client.NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>();
                        client.CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>();
                        client.CalledStationId = incoming.GetAttribute<CalledStationIdAttribute>();
                        client.AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>();

                        Console.WriteLine($"Updated {mac}");

                        if (client.NasIpAddress is null) break;

                        Console.WriteLine("Sleeping 5 seconds before Disconnect");
                        Thread.Sleep(5000);
                        Console.WriteLine("Attempting Disconnect");

                        vlanToAssign = "255";

                        RadiusPacket disconnect = RadiusPacket.Create(
                            RadiusCode.DISCONNECT_REQUEST,
                            ++lastSeenIdentifier,
                            null)
                            //.AddAttribute(client.UserName)
                            .AddAttribute(client.CallingStationId)
                            .AddAttribute(client.NasIpAddress)
                            .AddAttribute(client.NasIdentifier)
                            .AddAttribute(client.AccountingSessionId);
                        //.AddAttributes(vlanAssignmentAttributes)
                        //.AddAttribute(new TunnelPrivateGroupIdAttribute(0, "255"));

                        disconnect.ReplaceAuthenticator(disconnect.CalculateAuthenticator(secret));

                        udpClient.Send(disconnect.ToBytes(), new IPEndPoint(client.NasIpAddress.Address, 3799));

                        break;

                    case AccountingStatusTypeAttribute.StatusTypes.STOP:
                        clients = clients
                            .Where(x => x.ClientMAC != mac)
                            .ToList();
                        Console.WriteLine($"Removed {mac}");

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
