
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Data;
using System.Collections.Concurrent;

namespace CaptivePortal
{
    public class RadiusAuthorizationBackgroundService : BackgroundService
    {
        private UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1812));
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");
        private byte lastSeenIdentifier = 0;

        private string registrationVLAN = "255";
        private string authorizedVLAN = "254";

        private readonly RadiusAttributeParser parser;
        private readonly DatabaseService databaseService;

        public RadiusAuthorizationBackgroundService(
            RadiusAttributeParserService parserService, 
            DatabaseService databaseService)
        {
            parser = parserService.Parser;
            this.databaseService = databaseService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;

                RadiusPacket? incoming = null;
                try
                {
                    incoming = RadiusPacket.FromBytes(udpReceiveResult.Buffer, parser);
                }
                catch (RadiusException radEx)
                {
                    Console.WriteLine(radEx.Message);
                    continue;
                }

                lastSeenIdentifier = incoming.Identifier;

                switch (incoming.Code)
                {
                    case RadiusCode.ACCESS_REQUEST:

                        string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
                        if (mac is null)
                        {
                            await udpClient.SendAsync(RadiusPacket
                                .Create(RadiusCode.ACCESS_REJECT, incoming.Identifier)
                                .AddMessageAuthenticator(secret)
                                .AddResponseAuthenticator(secret, incoming.Authenticator)
                                .ToBytes(),
                                udpReceiveResult.RemoteEndPoint,
                                cancellationToken);
                            break;
                        }

                        _ = databaseService.Clients
                            .TryGetValue(mac, out Client? client);
                        if (client is null)
                        {
                            client = new Client();
                            _ = databaseService.Clients.TryAdd(mac, client);
                        }

                        if (client.AuthorizedUntil is null)
                        {
                            Console.WriteLine("REGISTRATION");
                            await udpClient.SendAsync(RadiusPacket
                                .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                                .AddAttribute(new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN))
                                .AddAttribute(new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802))
                                .AddAttribute(new TunnelPrivateGroupIdAttribute(0, registrationVLAN))
                                .AddMessageAuthenticator(secret)
                                .AddResponseAuthenticator(secret, incoming.Authenticator)
                                .ToBytes(),
                                udpReceiveResult.RemoteEndPoint,
                                cancellationToken);
                            break;
                        }

                        Console.WriteLine("AUTHORIZED");
                        await udpClient.SendAsync(RadiusPacket
                            .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                            .AddAttribute(new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN))
                            .AddAttribute(new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802))
                            .AddAttribute(new TunnelPrivateGroupIdAttribute(0, authorizedVLAN))
                            .AddMessageAuthenticator(secret)
                            .AddResponseAuthenticator(secret, incoming.Authenticator)
                            .ToBytes(),
                            udpReceiveResult.RemoteEndPoint,
                            cancellationToken);

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
