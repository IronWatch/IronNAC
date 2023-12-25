
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Database;
using Microsoft.EntityFrameworkCore;
using CaptivePortal.Database.Entities;
using CaptivePortal.Services;
using static System.Formats.Asn1.AsnWriter;

namespace CaptivePortal.Daemons
{
    public class RadiusAuthorizationDaemon(
        IConfiguration configuration,
        ILogger<RadiusAuthorizationDaemon> logger,
        RadiusAttributeParserService parser,
        IServiceProvider serviceProvider)
        : BaseDaemon<RadiusAuthorizationDaemon>(
            configuration,
            logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            using UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1812));
            using IServiceScope scope = serviceProvider.CreateScope();
            CaptivePortalDbContext db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();

            byte[] secret = Encoding.ASCII.GetBytes("thesecret");

            string registrationVLAN = "255";
            string authorizedVLAN = "254";

            Logger.LogInformation("{listener} started", nameof(RadiusAuthorizationDaemon));
            this.Running = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    RadiusPacket? incoming = null;
                    try
                    {
                        incoming = RadiusPacket.FromBytes(udpReceiveResult.Buffer, parser.Parser);
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

                            if (!device.Authorized ||
                                device.AuthorizedUntil <= DateTime.UtcNow)
                            {
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
                catch (SocketException sockEx)
                {
                    Logger.LogError(sockEx, "Socket Exception!");
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}
