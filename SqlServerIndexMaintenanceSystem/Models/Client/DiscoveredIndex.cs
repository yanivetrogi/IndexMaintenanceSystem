namespace SqlServerIndexMaintenanceSystem.Models.Client;

public class DiscoveredIndex
{
    public int DatabaseId { get; set; }
    public required string Schema { get; set; }
    public required string Table { get; set; }
    public required string Name { get; set; }
}