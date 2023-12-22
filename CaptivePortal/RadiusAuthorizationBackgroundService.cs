
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Database;
using Microsoft.EntityFrameworkCore;
using CaptivePortal.Database.Entities;

namespace CaptivePortal
{
    public class RadiusAuthorizationBackgroundService : BackgroundService
    {
        private UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1812));
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");

        private string registrationVLAN = "255";
        private string authorizedVLAN = "254";

        private readonly RadiusAttributeParser parser;
        private readonly IServiceProvider serviceProvider;

        public RadiusAuthorizationBackgroundService(
            RadiusAttributeParserService parserService,
            IServiceProvider serviceProvider)
        {
            parser = parserService.Parser;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                
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

                        CaptivePortalDbContext db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();

                        Device? device = await db.Devices
                            .Where(x => x.DeviceMac == mac)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (device is null)
                        {
                            device = new Device()
                            {
                                DeviceMac = mac
                            };

                            db.Add(device);
                            await db.SaveChangesAsync(cancellationToken);
                        }

                        if (device.AuthorizedUntil is null ||
                            device.AuthorizedUntil <= DateTime.UtcNow)
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
