namespace IndexMaintenanceSystem.Models.Ims
{
    public class Server
    {
        public int ServerId { get; set; }
        public required string Name { get; set; }
        public int MaxThreads { get; set; }
        public bool DiscoverDatabases { get; set; }
        public bool SearchIndexes { get; set; }
        public int? ScheduleId { get; set; }
        public bool ExcludeLastPartition { get; set; }
        public bool RunImmediately { get; set; }
        public byte? RebuildThreshold { get; set; }
        public byte? ReorganizeThreshold { get; set; }
        public bool? Online { get; set; }
        public byte? Maxdop { get; set; }
        public bool? SortInTempdb { get; set; }
        public int? IndexMinSizeKb { get; set; }
        public double? TlogSizeFactor { get; set; }
        public double? TlogGrowthFactor { get; set; }
        public int? DiskSafetyPct { get; set; }
        public int? DiskMinRemainingMb { get; set; }
        public bool? EnableTlogDiskCheck { get; set; }
        public bool? EnableTlogFileCheck { get; set; }
        public bool? EnableAlwaysOnCheck { get; set; }
        public bool IntegratedSecurity { get; set; }
        public bool Active { get; set; }

        public override string ToString()
        {
            return $"[{ServerId}] {Name}";
        }
    }
}

