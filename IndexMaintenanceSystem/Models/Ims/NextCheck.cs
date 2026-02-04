namespace IndexMaintenanceSystem.Models.Ims;

public class NextCheck
{
    public int ScheduleId { get; set; }
    public int ServerId { get; set; }
    public int? DatabaseId { get; set; }
    public int? IndexId { get; set; }
    public int? NextExecutionDate { get; set; } // null can be returned by the sp_get_schedule_next_execution_date_and_time
    public int? NextExecutionTime { get; set; } // null can be returned by the sp_get_schedule_next_execution_date_and_time
    public int? PreviousExecutionDate { get; set; }
    public int? PreviousExecutionTime { get; set; }

    public DateTime? NextExecutionDateTime => NextExecutionDate.HasValue && NextExecutionTime.HasValue
            ? ImsDateTimeHelper.ConvertIntToDateTime(NextExecutionDate.Value, NextExecutionTime.Value)
            : null;

    public DateTime? PreviousExecutionDateTime => PreviousExecutionDate.HasValue && PreviousExecutionTime.HasValue
            ? ImsDateTimeHelper.ConvertIntToDateTime(PreviousExecutionDate.Value, PreviousExecutionTime.Value)
            : null;

    public bool RequiresRescheduling(int toleranceInSeconds)
    {
        if (!NextExecutionDateTime.HasValue)
        {
            return false;
        }

        return NextExecutionDateTime.Value.AddSeconds(toleranceInSeconds) < DateTime.Now;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        
        var other = (NextCheck)obj;
        return ScheduleId == other.ScheduleId &&
                ServerId == other.ServerId &&
                DatabaseId == other.DatabaseId &&
                IndexId == other.IndexId;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + ScheduleId;
            hash = hash * 23 + ServerId;
            hash = hash * 23 + (DatabaseId ?? 0);
            hash = hash * 23 + (IndexId ?? 0);
            return hash;
        }
    }
}
