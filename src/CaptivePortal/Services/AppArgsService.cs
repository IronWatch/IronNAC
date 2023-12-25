namespace CaptivePortal.Services
{
    public class AppArgsService
    {
        public string[] Args { get; private set; }

        public AppArgsService(string[] args)
        {
            this.Args = args;
        }
    }
}
