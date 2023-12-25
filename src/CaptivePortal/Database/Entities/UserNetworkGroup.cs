namespace CaptivePortal.Database.Entities
{
    public class UserNetworkGroup
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        private User? _user;
        public User User
        {
            get => _user ?? throw new InvalidOperationException();
            set => _user = value;
        }

        public int NetworkGroupId { get; set; }
        private NetworkGroup? _networkGroup;
        public NetworkGroup NetworkGroup
        {
            get => _networkGroup ?? throw new InvalidOperationException();
            set => _networkGroup = value;
        }
    }
}
