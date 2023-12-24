using CaptivePortal.Models;

namespace CaptivePortal.Database.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Hash { get; set; }
        public bool ChangePasswordNextLogin { get; set; }
        public PermissionLevel PermissionLevel { get; set; }

        public List<Device> Devices { get; set; } = new();
        public List<UserSession> UserSessions { get; set; } = new();
    }
}
