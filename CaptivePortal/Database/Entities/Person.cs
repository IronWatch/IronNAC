namespace CaptivePortal.Database.Entities
{
    public class Person
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }

        public List<Device> Devices { get; set; } = new();
    }
}
