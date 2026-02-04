namespace IndexMaintenanceSystem.Models.Ims
{
    public class Database
    {
        public int DatabaseId { get; set; }
        public required string Name { get; set; }
        public int MaxThreads { get; set; }
        public bool? SearchIndexes { get; set; }
        public int? ScheduleId { get; set; }
        public bool? ExcludeLastPartition { get; set; }
        public bool? RunImmediately { get; set; }
        public byte? RebuildThreshold { get; set; }
        public byte? ReorganizeThreshold { get; set; }
        public bool? Online { get; set; }
        public byte? Maxdop { get; set; }
        public bool? SortInTempdb { get; set; }
        public int? IndexMinSizeKb { get; set; }
        public double? TlogSizeFactor { get; set; }
        public double? TlogGrowthFactor { get; set; }
        public bool? EnableAlwaysOnCheck { get; set; }

        public bool Active { get; set; }

        public int ServerId { get; set; }

        public override string ToString()
        {
            return $"[{DatabaseId}] {Name}";
        }
    }
}

