using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class DeviceNetwork
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Device))]
        public int DeviceId { get; set; }
        public required Device Device { get; set; }

        public int NetworkId { get; set; }
        public required Network Network { get; set; }

        public required string DeviceAddress { get; set; }
        public bool ManuallyAssignedAddress { get; set; }

        public DateTime? LeaseIssuedAt { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
    }
}
