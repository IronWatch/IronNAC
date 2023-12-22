using Radius.RadiusAttributes;

namespace CaptivePortal.Data
{
    public class Session
    {
        public FramedIpAddressAttribute? FramedIpAddress { get; set; }
        public NasIpAddressAttribute? NasIpAddress { get; set; }
        public NasIdentifierAttribute? NasIdentifier { get; set; }
        public CallingStationIdAttribute? CallingStationId { get; set; }
        public AccountingSessionIdAttribute? AccountingSessionId { get; set; }
    }
}
