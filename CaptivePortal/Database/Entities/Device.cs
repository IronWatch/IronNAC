namespace CaptivePortal.Database.Entities
{
    public class Device
    {
        public int Id { get; set; }

        public int? PersonId { get; set; }
        public Person? Person { get; set; }

        public DateTime? AuthorizedUntil { get; set; }

        public string? DeviceMac { get; set; }
        public string? DeviceIpAddress { get; set; }

        public string? NasIpAddress { get; set; }
        public string? NasIdentifier { get; set; }
        public string? CallingStationId { get; set; }
        public string? AccountingSessionId { get; set; }
    }
}
