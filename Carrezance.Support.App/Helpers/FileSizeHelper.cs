namespace Carrezance.Support.App.Helpers;

public static class FileSizeHelper
{
    public static string Format(long bytes)
    {
        if (bytes == 0)
        {
            return "0 octet";
        }

        string[] units = ["o", "Ko", "Mo", "Go", "To"];
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
