using CaptivePortal.Data;
using System.Collections.Concurrent;

namespace CaptivePortal
{
    public class DatabaseService
    {
        public ConcurrentDictionary<string, Client> Clients { get; set; } = new();
        public ConcurrentDictionary<string, Session> Sessions { get; set; } = new();
    }
}
