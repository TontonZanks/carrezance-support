namespace Carrezance.Support.App.Helpers;

public static class DateTimeHelper
{
    public static string FormatDuration(TimeSpan value)
    {
        return value.TotalDays >= 1
            ? $"{(int)value.TotalDays} j {value.Hours} h {value.Minutes} min"
            : $"{value.Hours} h {value.Minutes} min";
    }
}
