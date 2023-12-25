
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
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

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
            UdpClient? udpClient = null;
            IServiceScope? scope = null;
            byte[] secret;
            string fallbackVlan;
            CaptivePortalDbContext db;

            try
            {
                try
                {
                    string listenAddress = Configuration.GetValue<string>("Radius:ListenAddress")
                        ?? throw new MissingFieldException("Radius:ListenAddress");

                    string? secretString = Configuration.GetValue<string>("Radius:AuthorizationSecret")
                        ?? throw new MissingFieldException("Radius:AuthorizationSecret");
                    secret = Encoding.ASCII.GetBytes(secretString);

                    fallbackVlan = Configuration.GetValue<string>("CaptivePortal:FallbackRegistrationVlan")
                        ?? throw new MissingFieldException("CaptivePortal:FallbackRegistrationVlan");

                    udpClient = new(new IPEndPoint(IPAddress.Parse(listenAddress), 1812));
                    scope = serviceProvider.CreateScope();
                    db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Critical error when starting the {listener}!", nameof(RadiusAuthorizationDaemon));
                    return;
                }

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
                        RadiusPacket response;

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
                                    .Include(x => x.DeviceNetwork)
                                        .ThenInclude(x => x!.Network)
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

                                // Not Authorized so go to registration
                                if (!device.Authorized ||
                                    device.AuthorizedUntil <= DateTime.UtcNow)
                                {
                                    response = RadiusPacket
                                        .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                                        .AddAttribute(new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN))
                                        .AddAttribute(new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802))
                                        .AddAttribute(new TunnelPrivateGroupIdAttribute(0, fallbackVlan))
                                        .AddMessageAuthenticator(secret)
                                        .AddResponseAuthenticator(secret, incoming.Authenticator);

                                    await udpClient.SendAsync(
                                        response.ToBytes(),
                                        udpReceiveResult.RemoteEndPoint,
                                        cancellationToken);
                                    break;
                                }

                                // Authorized
                                string authorizedVlan = device.DeviceNetwork?.Network.Vlan.ToString() ?? fallbackVlan;

                                response = RadiusPacket
                                    .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                                    .AddAttribute(new TunnelTypeAttribute(0, TunnelTypeAttribute.TunnelTypes.VLAN))
                                    .AddAttribute(new TunnelMediumTypeAttribute(0, TunnelMediumTypeAttribute.Values.IEEE_802))
                                    .AddAttribute(new TunnelPrivateGroupIdAttribute(0, authorizedVlan))
                                    .AddMessageAuthenticator(secret)
                                    .AddResponseAuthenticator(secret, incoming.Authenticator);


                                await udpClient.SendAsync(
                                    response.ToBytes(),
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
            finally 
            { 
                udpClient?.Dispose();
                scope?.Dispose();
            }
        }
    }
}
