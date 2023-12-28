using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace CaptivePortal.Daemons
{
    public abstract class BaseDaemon<T> : BackgroundService where T : BaseDaemon<T>
    {
        protected readonly ILogger Logger;

        protected CancellationTokenSource StoppingTokenSource = new();
        protected SemaphoreSlim StoppedSemaphore = new(0, 1);
        protected SemaphoreSlim RestartSemaphore = new(0, 1);

        public bool Running { get; protected set; }

        protected TimeSpan StopTimeout = TimeSpan.FromSeconds(60);

        public BaseDaemon(ILogger logger)
        {
            this.Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{daemon} start requested by application host", typeof(T).Name);

            while (!cancellationToken.IsCancellationRequested)
            {
                await EntryPoint(StoppingTokenSource.Token);

                Logger.LogInformation("{daemon} stopped", typeof(T).Name);
                this.Running = false;
                StoppedSemaphore.Release();

                // stopped, block until we are released to restart
                await RestartSemaphore.WaitAsync(cancellationToken);
            }
        }

        public virtual void StartDaemon()
        {
            Logger.LogInformation("{daemon} start requested", typeof(T).Name);

            if (Running)
            {
                Logger.LogInformation("{daemon} already running", typeof(T).Name);
                return;
            }

            StoppingTokenSource = new();
            try
            {
                RestartSemaphore.Release();
            }
            catch (SemaphoreFullException) { }
        }

        public virtual Task<bool> StopDaemonAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("{daemon} stop requested", typeof(T).Name);

            if (!Running)
            {
                Logger.LogInformation("{daemon} already stopped", typeof(T).Name);
                return Task.FromResult(true);
            }

            StoppingTokenSource.Cancel();
            return StoppedSemaphore.WaitAsync(StopTimeout, cancellationToken);
        }

        protected abstract Task EntryPoint(CancellationToken cancellationToken);
    }
}
