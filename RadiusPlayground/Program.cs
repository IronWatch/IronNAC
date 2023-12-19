using System.Net;
using System.Net.Sockets;
using System.Text;
using Radius;
using Radius.RadiusAttributes;

UdpClient accounting = new(new IPEndPoint(IPAddress.Any, 1812));
IPEndPoint accountingRemoteEndpoint = new(IPAddress.Any, 0);
IPEndPoint controllerCoAEndpoint = new(IPAddress.Parse("10.100.0.1"), 3799);


byte[] secret = Encoding.ASCII.GetBytes("thesecret");
byte lastSeenIdentifier = 0;

RadiusAttributeParser parser = new();
parser.AddDefault();

Console.WriteLine($"Discovered {parser.Types.Count} parseable IRadiusAttribute Types\n");

try
{
    while (true)
    {
        byte[] packet = accounting.Receive(ref accountingRemoteEndpoint);
        RadiusPacket? request = null;

        try
        {
            request = RadiusPacket.FromBytes(packet, parser);
        }
        catch (RadiusException radEx)
        {
            Console.WriteLine(radEx.Message);
            continue;
        }

        Console.WriteLine($"Received {request.Code}");

        lastSeenIdentifier = request.Identifier;

        if (request.Code != RadiusCode.ACCESS_REQUEST) continue;

        string? username = request.GetAttribute<UserNameAttribute>()?.Username;

        Console.Write($"Request {request.Identifier} for {username} ");
        Console.WriteLine("ACCEPTED");

        IRadiusAttribute[] vlanAssignmentAttributes = [
            new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN),
            new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802)
        ];

        RadiusPacket response = RadiusPacket.Create(
            RadiusCode.ACCESS_ACCEPT,
            request.Identifier,
            attributes: vlanAssignmentAttributes)
            .AddAttribute(new TunnelPrivateGroupIdAttribute(0, "254"))
            .AddMessageAuthenticator(secret)
            .ReplaceAuthenticator(request.Authenticator)
            .Sign(secret);

        byte[] responseBytes = response.ToBytes();

        accounting.Send(responseBytes, accountingRemoteEndpoint);

        /*
        if (++lastSeenIdentifier > byte.MaxValue) lastSeenIdentifier = 0;

        // We need accounting attributes here too so this won't work right now
        RadiusPacket coa = RadiusPacket.Create(
            RadiusCode.COA_REQUEST,
            lastSeenIdentifier)
            .AddAttribute(request.GetAttribute(RadiusAttributeType.USER_NAME))
            .AddAttribute(request.GetAttribute(RadiusAttributeType.NAS_IP_ADDRESS))
            .AddAttribute(request.GetAttribute(RadiusAttributeType.NAS_IDENTIFIER))
            .AddAttribute(request.GetAttribute(RadiusAttributeType.CALLING_STATION_ID))
            .AddAttributes(vlanAttributes)
            .AddAttribute(vlan254)
            .AddMessageAuthenticator(secret)
            .Sign(secret);

        await Task.Delay(5000);

        accounting.Send(coa.ToBytes(), controllerCoAEndpoint);*/
    }
}
catch (SocketException sockEx)
{
    Console.WriteLine(sockEx.Message);
}
finally
{
    accounting.Close();
}
