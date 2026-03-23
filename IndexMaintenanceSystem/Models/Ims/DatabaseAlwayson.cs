namespace IndexMaintenanceSystem.Models.Ims;

public class DatabaseAlwayson
{
    public required string Server { get; set; }
    public required bool IntegratedSecurity { get; set; }
    public required string Database { get; set; }
    public required string AgName { get; set; }
}
