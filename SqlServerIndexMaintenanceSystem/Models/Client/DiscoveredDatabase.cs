namespace SqlServerIndexMaintenanceSystem.Models.Client
{
    public class DiscoveredDatabase
    {
        public int ServerId { get; set; }
        public required string Name { get; set; }
    }
}