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

        public List<Network> Networks { get; set; } = new();
        public List<UserNetworkGroup> UserNetworkGroups { get; set; } = new();

        [NotMapped]
        public string? Name =>
            Registration ? "Registration Network" :
            Guest ? "Guest Network Pool" :
            CustomName;

        [NotMapped]
        public int TotalCapacity =>
            Networks.Sum(x => x.Capacity);

        [NotMapped]
        public int TotalDevices =>
            Networks.Sum(x => x.DeviceNetworks.Count);

        [NotMapped]
        public bool Full =>
            Networks.All(x => x.Full);

        [NotMapped]
        public float FillPercentage
        {
            get
            {
                if (TotalDevices == 0) return 0f;

                return (float)TotalCapacity / (float)TotalDevices;
            }
        }
    }
}
