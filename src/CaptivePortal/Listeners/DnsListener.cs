
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using DNS.ResourceRecords;
using DNS;

namespace CaptivePortal.Listeners
{
    public class DnsListener : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        private Thread? thread;
        private CancellationTokenSource cancellationTokenSource = new();

        public bool Running => thread?.IsAlive ?? false;

        public DnsListener(IConfiguration configuration, ILogger<DnsListener> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Start();
            return Task.CompletedTask;
        }

        public void Start()
        {
            logger.LogInformation($"{nameof(DnsListener)} start requested");
            if (thread is not null && thread.IsAlive)
            {
                logger.LogError($"{nameof(DnsListener)} thread is already running!");
                throw new InvalidOperationException($"{nameof(DnsListener)} thread is already running!");
            }

            thread = new Thread(async _ => await DnsServer(cancellationTokenSource.Token));
            thread.IsBackground = true;
            thread.Start();
        }

        public bool Stop()
        {
            logger.LogInformation($"{nameof(DnsListener)} stop requested");
            if (thread is null || !thread.IsAlive)
            {
                logger.LogInformation($"{nameof(DnsListener)} already stopped");
                return true;
            }

            logger.LogInformation($"{nameof(DnsListener)} requesting thread cancellation and waiting 60 seconds for thread to join");
            cancellationTokenSource.Cancel();
            bool joinedBeforeTimeout = thread.Join(TimeSpan.FromSeconds(60));

            if (joinedBeforeTimeout)
            {
                logger.LogInformation($"{nameof(DnsListener)} stopped");
                return true;
            }

            logger.LogWarning($"{nameof(DnsListener)} did not stop before timeout!");
            return false;
        }

        private async Task DnsServer(CancellationToken cancellationToken)
        {
            logger.LogInformation("{listener} thread is starting!", nameof(DnsListener));
            IPAddress redirectAddress;
            UdpClient? udpClient = null;
            
            // udpClient disposal
            try
            {
                // Thread startup error checking
                try
                {
                    string listenAddress = configuration.GetValue<string>("DnsServer:ListenAddress")
                        ?? throw new MissingFieldException("DnsServer:ListenAddress");

                    string redirectAddressString = configuration.GetValue<string>("DnsServer:RedirectAddress")
                        ?? throw new MissingFieldException("DnsServer:RedirectAddress");
                    redirectAddress = IPAddress.Parse(redirectAddressString);

                    udpClient = new(new IPEndPoint(IPAddress.Parse(listenAddress), 53));
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Critical error when starting the {listener} thread!", nameof(DnsListener));
                    return;
                }

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
                    catch (SocketException) { }
                }
            }
            finally
            {
                udpClient?.Dispose();
            }
        }
    }
}
