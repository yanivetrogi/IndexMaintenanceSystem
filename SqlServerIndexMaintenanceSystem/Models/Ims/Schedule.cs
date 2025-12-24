namespace SqlServerIndexMaintenanceSystem.Models.Ims;

public class Schedule {
    public int ScheduleId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int FreqType { get; set; }
    public int FreqInterval { get; set; }
    public int FreqSubdayType { get; set; }
    public int FreqSubdayInterval { get; set; }
    public int FreqRelativeInterval { get; set; }
    public int FreqRecurrenceFactor { get; set; }
    public int ActiveStartDate { get; set; }
    public int ActiveStartTime { get; set; }
    public int ActiveEndDate { get; set; }
    public int ActiveEndTime { get; set; }
    public DateTime DateCreated { get; set; }

    public DateTime ActiveStartDateTime => ImsDateTimeHelper.ConvertIntToDateTime(ActiveStartDate, ActiveStartTime);
    public DateTime ActiveEndDateTime => ImsDateTimeHelper.ConvertIntToDateTime(ActiveEndDate, ActiveEndTime);

    
    public override string ToString()
    {
        return $"[{ScheduleId}] \"{Description}\"";
    }
}
