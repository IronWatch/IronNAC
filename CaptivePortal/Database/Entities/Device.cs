namespace CaptivePortal.Database.Entities
{
    public class Device
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? DeviceNetworkId { get; set; }
        public DeviceNetwork? DeviceNetwork { get; set; }

        public string? NickName { get; set; }

        public bool Authorized { get; set; }
        public DateTime? AuthorizedUntil { get; set; }

        public string? DeviceMac { get; set; }
        public string? DetectedDeviceIpAddress { get; set; }

        public string? NasIpAddress { get; set; }
        public string? NasIdentifier { get; set; }
        public string? CallingStationId { get; set; }
        public string? AccountingSessionId { get; set; }
    }
}
