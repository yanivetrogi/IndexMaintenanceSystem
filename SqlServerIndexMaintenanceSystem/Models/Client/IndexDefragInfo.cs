namespace SqlServerIndexMaintenanceSystem.Models.Client
{
    public class IndexDefragInfo
    {
        public string? Action { get; set; }
        public long SizeKB { get; set; }
        public byte AvgFragmentationPercent { get; set; }
        public string? Command { get; set; }
    }
}