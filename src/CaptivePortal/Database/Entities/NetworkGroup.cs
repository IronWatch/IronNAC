using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class NetworkGroup
    {
        public int Id { get; set; }
        public bool Registration { get; set; }
        public bool Guest { get; set; }
        public string? CustomName { get; set; }
        public bool IsPool { get; set; }

        [NotMapped]
        public string? Name =>
            Registration ? "Registration Network" :
            Guest ? "Guest Network Pool" :
            CustomName;

        public List<Network> Networks { get; set; } = new();
        public List<UserNetworkGroup> UserNetworkGroups { get; set; } = new();
    }
}
