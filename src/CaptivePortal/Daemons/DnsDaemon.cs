
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using DNS.ResourceRecords;
using DNS;

namespace CaptivePortal.Daemons
{
    public class DnsDaemon : BaseDaemon<DnsDaemon>
    {
        public readonly Guid guid = Guid.NewGuid();
        
        public DnsDaemon(IConfiguration configuration, ILogger<DnsDaemon> logger)
            : base(configuration, logger) { }

        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            IPAddress redirectAddress;
            UdpClient? udpClient = null;
            
            // udpClient disposal
            try
            {
                // Thread startup error checking
                try
                {
                    string listenAddress = Configuration.GetValue<string>("DnsServer:ListenAddress")
                        ?? throw new MissingFieldException("DnsServer:ListenAddress");

                    string redirectAddressString = Configuration.GetValue<string>("DnsServer:RedirectAddress")
                        ?? throw new MissingFieldException("DnsServer:RedirectAddress");
                    redirectAddress = IPAddress.Parse(redirectAddressString);

                    udpClient = new(new IPEndPoint(IPAddress.Parse(listenAddress), 53));
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Critical error when starting the {listener} thread!", nameof(DnsDaemon));
                    return;
                }

                Logger.LogInformation("{listener} started", nameof(DnsDaemon));
                this.Running = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                        if (cancellationToken.IsCancellationRequested) break;

                        DnsPacket request = DnsPacket.FromBytes(udpReceiveResult.Buffer);

                        DnsPacket response = new DnsPacket();
                        response.TransactionId = request.TransactionId;
                        response.Flags.IsResponse = true;
                        response.Flags.IsRecursionDesired = request.Flags.IsRecursionDesired;
                        response.Questions = request.Questions;

                        if (request.NumQuestions != 1 ||
                            request.Questions[0].Type != DnsResourceRecordTypes.A)
                        {
                            response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.REFUSED;
                        }
                        else
                        {
                            response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.NO_ERROR;
                            response.Answers.Add(new ARecord(request.Questions[0].Name, redirectAddress, 60));
                        }

                        await udpClient.SendAsync(response.ToBytes(), udpReceiveResult.RemoteEndPoint, cancellationToken);
                    }
                    catch (SocketException ex)
                    {
                        Logger.LogError(ex, "Socket Exception!");
                    }
                    catch (OperationCanceledException) { }
                }
            }
            finally
            {
                udpClient?.Dispose();
            }
        }
    }
}
