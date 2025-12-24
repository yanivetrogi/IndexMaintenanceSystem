using SqlServerIndexMaintenanceSystem.Models.Client;

namespace SqlServerIndexMaintenanceSystem.Models.Ims;

public enum HistoryEntrySkipped
{
    NOT_NEEDED,
    INACTIVE,
    INDEX_MIN_SIZE,
    TLOG_SIZE,
    TLOG_DISK_SAFETY_PERCENT,
    DISK_MIN_REMAINING_SPACE,
    RUN_IMMEDIATELY_DISABLED,
    OWN_SCHEDULE,
}

public class HistoryEntry : ICloneable
{
    public Guid Guid { get; set; }

    public required string Reason { get; set; }
    public string? Action { get; set; }
    public string? Error { get; set; }
    public required string Server { get; set; }
    public required string Database { get; set; }
    public required string Schema { get; set; }
    public required string Table { get; set; }
    public required string Index { get; set; }
    public int? ObjectId { get; set; }
    public int? IndexId { get; set; }
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
    public int? PartitionNumber { get; set; }

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long? SizeKBBefore { get; set; }
    public long? SizeKBAfter { get; set; }
    public byte? AvgFragmentationPercentBefore { get; set; }
    public byte? AvgFragmentationPercentAfter { get; set; }
    public string? Command { get; set; }

    private HistoryEntry() { }

    public HistoryEntry(Guid guid)
    {
        Guid = guid;
    }

    public void UpdateWithSysInfo(IndexSysInfo sysInfo)
    {
        ObjectId = sysInfo.ObjectId;
        IndexId = sysInfo.IndexId;
    }

    public void UpdateWithStartDefragInfo(IndexDefragInfo startInfo, DateTime? startTime, int? partitionNumber = null)
    {
        Action = startInfo.Action;
        SizeKBBefore = startInfo.SizeKB;
        AvgFragmentationPercentBefore = startInfo.AvgFragmentationPercent;
        Command = startInfo.Command;
        StartTime = startTime;
        PartitionNumber = partitionNumber;
    }

    public void MarkAsCompleted(IndexDefragInfo? finalInfo, DateTime startTime, DateTime endTime)
    {
        Error = null;
        StartTime = startTime;
        EndTime = endTime;
        AvgFragmentationPercentAfter = finalInfo?.AvgFragmentationPercent;
        SizeKBAfter = finalInfo?.SizeKB;
    }

    public void MarkAsFailed(DateTime? endTime, string error)
    {
        Error = error;
        EndTime = endTime;
    }

    public void MarkAsSkipped(HistoryEntrySkipped status, string message)
    {
        Action = "SKIPPED_" + Enum.GetName(status);
        Error = message;
    }

    public object Clone()
    {
        return new HistoryEntry()
        {
            Guid = this.Guid,
            Server = this.Server,
            Database = this.Database,
            Schema = this.Schema,
            Table = this.Table,
            Index = this.Index,
            Reason = this.Reason,
            Action = this.Action,
            Error = this.Error,
            ObjectId = this.ObjectId,
            IndexId = this.IndexId,
            RebuildThreshold = this.RebuildThreshold,
            ReorganizeThreshold = this.ReorganizeThreshold,
            Online = this.Online,
            Maxdop = this.Maxdop,
            SortInTempdb = this.SortInTempdb,
            IndexMinSizeKb = this.IndexMinSizeKb,
            PartitionNumber = this.PartitionNumber,
            StartTime = this.StartTime,
            EndTime = this.EndTime,
            SizeKBBefore = this.SizeKBBefore,
            SizeKBAfter = this.SizeKBAfter,
            AvgFragmentationPercentBefore = this.AvgFragmentationPercentBefore,
            AvgFragmentationPercentAfter = this.AvgFragmentationPercentAfter,
            Command = this.Command
        };
    }
}
