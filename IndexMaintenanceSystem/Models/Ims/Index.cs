namespace IndexMaintenanceSystem.Models.Ims
{
    public class Index
    {
        public int IndexId { get; set; }
        public required string Schema { get; set; }
        public required string Table { get; set; }
        public string? Name { get; set; }
        public bool? ExcludeLastPartition { get; set; }
        public bool? RunImmediately { get; set; }
        public int? ScheduleId { get; set; }
        public byte? RebuildThreshold { get; set; }
        public byte? ReorganizeThreshold { get; set; }
        public bool? Online { get; set; }
        public byte? Maxdop { get; set; }
        public bool? SortInTempdb { get; set; }
        public int? IndexMinSizeKb { get; set; }
        public double? TlogSizeFactor { get; set; }
        public double? TlogGrowthFactor { get; set; }
        public bool Active { get; set; }

        public int DatabaseId { get; set; }

        public override string ToString()
        {
            var id = IndexId > 0 ? IndexId.ToString() : "searched";
            return $"[{id}] {Table}:{Name}";
        }

        public Index DeepCopy()
        {
            return new Index
            {
                IndexId = IndexId,
                Schema = Schema,
                Table = Table,
                Name = Name,
                ExcludeLastPartition = ExcludeLastPartition,
                RunImmediately = RunImmediately,
                ScheduleId = ScheduleId,
                RebuildThreshold = RebuildThreshold,
                ReorganizeThreshold = ReorganizeThreshold,
                Online = Online,
                Maxdop = Maxdop,
                SortInTempdb = SortInTempdb,
                IndexMinSizeKb = IndexMinSizeKb,
                TlogSizeFactor = TlogSizeFactor,
                TlogGrowthFactor = TlogGrowthFactor,
                Active = Active,
                DatabaseId = DatabaseId
            };
        }
    }
}

