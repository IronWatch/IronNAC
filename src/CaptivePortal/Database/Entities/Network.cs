using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class Network
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string NetworkAddress { get; set; }
        public required string GatewayAddress { get; set; }
        public int Cidr { get; set; }
        public int Vlan { get; set; }
        public int Capacity { get; set; }

        public int NetworkGroupId { get; set; }
        private NetworkGroup? _networkGroup;
        public NetworkGroup NetworkGroup
        {
            set => _networkGroup = value;
            get => _networkGroup ?? throw new InvalidOperationException();
        }

        public List<DeviceNetwork> DeviceNetworks { get; set; } = new();

        [NotMapped]
        public bool Full => DeviceNetworks.Count >= Capacity;

        [NotMapped]
        public float FillPercentage
        {
            get
            {
                if (DeviceNetworks.Count == 0) return 0f;

                return (float)Capacity / (float)DeviceNetworks.Count;
            }
        }
    }
}
