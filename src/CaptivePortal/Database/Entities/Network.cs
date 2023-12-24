namespace CaptivePortal.Database.Entities
{
    public class Network
    {
        public int Id { get; set; }
        public required string NetworkAddress { get; set; }
        public required string GatewayAddress { get; set; }
        public int Cidr { get; set; }
        public int Vlan { get; set; }

        public List<DeviceNetwork> DeviceNetworks { get; set; } = new();
    }
}
