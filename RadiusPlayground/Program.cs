using System.Net;
using System.Net.Sockets;
using System.Text;
using Radius;

UdpClient accounting = new(new IPEndPoint(IPAddress.Any, 1812));
IPEndPoint accountingRemoteEndpoint = new(IPAddress.Any, 0);

byte[] secret = Encoding.ASCII.GetBytes("thesecret");
byte[] vlan = Encoding.ASCII.GetBytes("254");

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

        string? username = request.ReadString(RadiusAttributeType.USER_NAME);

        Console.Write($"Request {request.Identifier} for {username} ");
        Console.WriteLine("ACCEPTED");

        List<RadiusAttribute> attributes = [
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_TYPE, [0,0,0,13]),
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_MEDIUM_TYPE, [0, 0, 0, 6]),
            RadiusAttribute.Build(RadiusAttributeType.TUNNEL_PRIVATE_GROUP_ID, vlan.Prepend<byte>(0).ToArray())
        ];

        accounting.Send(request.CreateResponse(RadiusCode.ACCESS_ACCEPT, secret, attributes.ToArray()), accountingRemoteEndpoint);
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
