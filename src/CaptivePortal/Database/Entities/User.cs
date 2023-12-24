namespace CaptivePortal.Database.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Hash { get; set; }
        public bool ChangePasswordNextLogin { get; set; }
        public bool IsStaff { get; set; }
        public bool IsAdmin { get; set; }

        public List<Device> Devices { get; set; } = new();
        public List<UserSession> UserSessions { get; set; } = new();
    }
}
