using UnityEngine;

public static class GlobalFormatter
{
    public static string FormatTimeMinSec(float timeInSeconds)
    {
        if (timeInSeconds <= 0)
            return "0:00";

        int minutes = (int)(timeInSeconds / 60);
        int seconds = (int)(timeInSeconds % 60);

        return $"{minutes}:{seconds:00}";
    }

    public static string FormatTimeSecMilli(float timeInSeconds)
    {
        if (timeInSeconds <= 0)
            return "00:000";

        int seconds = (int)timeInSeconds;
        int milliseconds = (int)((timeInSeconds - (int)timeInSeconds) * 1000);

        return $"{seconds:00}:{milliseconds:000}";
    }
}
