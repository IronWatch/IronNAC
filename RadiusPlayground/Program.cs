using System.Net;
using System.Net.Sockets;
using System.Text;
using Radius;

UdpClient accounting = new(new IPEndPoint(IPAddress.Any, 1812));
IPEndPoint accountingRemoteEndpoint = new(IPAddress.Any, 0);
IPEndPoint controllerCoAEndpoint = new(IPAddress.Parse("10.100.0.1"), 3799);


byte[] secret = Encoding.ASCII.GetBytes("thesecret");
byte lastSeenIdentifier = 0;

try
{
    while (true)
    {
        byte[] packet = accounting.Receive(ref accountingRemoteEndpoint);
        RadiusPacket? request = null;

        try
        {
            request = RadiusPacket.FromBytes(packet);
        }
        catch (RadiusException radEx)
        {
            Console.WriteLine(radEx.Message);
            continue;
        }

        Console.WriteLine($"Received {request.Code}");

        lastSeenIdentifier = request.Identifier;

        if (request.Code != RadiusCode.ACCESS_REQUEST) continue;

        string? username = request.GetAttributeString(RadiusAttributeType.USER_NAME);

        Console.Write($"Request {request.Identifier} for {username} ");
        Console.WriteLine("ACCEPTED");

        RadiusAttribute[] vlanAttributes = [
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_TYPE, [0,0,0,13]),
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_MEDIUM_TYPE, [0, 0, 0, 6])
        ];

        RadiusAttribute vlan255 =
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_PRIVATE_GROUP_ID, Encoding.ASCII.GetBytes("255").Prepend<byte>(0).ToArray());

        RadiusAttribute vlan254 =
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_PRIVATE_GROUP_ID, Encoding.ASCII.GetBytes("254").Prepend<byte>(0).ToArray());

        RadiusPacket response = RadiusPacket.Create(
            RadiusCode.ACCESS_ACCEPT,
            request.Identifier,
            attributes: vlanAttributes)
            .AddAttribute(vlan255)
            .AddMessageAuthenticator(secret)
            .ReplaceAuthenticator(request.Authenticator)
            .Sign(secret);

        accounting.Send(response.ToBytes(), accountingRemoteEndpoint);

        if (++lastSeenIdentifier > byte.MaxValue) lastSeenIdentifier = 0;

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

        accounting.Send(coa.ToBytes(), controllerCoAEndpoint);
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
