namespace SqlServerIndexMaintenanceSystem.Models.Ims;

public class ImsDateTimeHelper
{
    public static DateTime ConvertIntToDateTime(int date, int time)
    {
        var dateStr = date.ToString("D8");
        var timeStr = time.ToString("D6");

        var year = int.Parse(dateStr.Substring(0, 4));
        var month = int.Parse(dateStr.Substring(4, 2));
        var day = int.Parse(dateStr.Substring(6, 2));

        var hour = int.Parse(timeStr.Substring(0, 2));
        var minute = int.Parse(timeStr.Substring(2, 2));
        var second = int.Parse(timeStr.Substring(4, 2));

        return new DateTime(year, month, day, hour, minute, second);
    }
}