
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal
{
    public class RadiusAccountingBackgroundService : BackgroundService
    {
        private UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 1813));
        private byte[] secret = Encoding.ASCII.GetBytes("thesecret");
        private byte lastSeenIdentifier = 0;

        private readonly RadiusAttributeParser parser;
        private readonly IServiceProvider serviceProvider;

        public RadiusAccountingBackgroundService(
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

                lastSeenIdentifier = incoming.Identifier;

                switch (incoming.Code)
                {
                    case RadiusCode.ACCOUNTING_REQUEST:

                        // We got an accounting message, acknowledge it
                        await udpClient.SendAsync(RadiusPacket
                            .Create(RadiusCode.ACCOUNTING_RESPONSE, incoming.Identifier)
                            .AddMessageAuthenticator(secret)
                            .AddResponseAuthenticator(secret, incoming.Authenticator)
                            .ToBytes(),
                            udpReceiveResult.RemoteEndPoint,
                            cancellationToken);

                        string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
                        if (mac is null) break;

                        AccountingStatusTypeAttribute.StatusTypes? statusType = incoming.GetAttribute<AccountingStatusTypeAttribute>()?.StatusType;
                        if (statusType is null) break;

                        CaptivePortalDbContext db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();

                        Device? device = await db.Devices
                            .Where(x => x.DeviceMac == mac)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (device is null) break;

                        if (statusType == AccountingStatusTypeAttribute.StatusTypes.START ||
                            statusType == AccountingStatusTypeAttribute.StatusTypes.INTERIM_UPDATE)
                        {
                            device.DetectedDeviceIpAddress = incoming.GetAttribute<FramedIpAddressAttribute>()?.Address.ToString();
                            device.NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>()?.Address.ToString();
                            device.NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>()?.Value;
                            device.CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>()?.Value;
                            device.AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>()?.Value;
                        }

                        await db.SaveChangesAsync(cancellationToken);

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
