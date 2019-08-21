using System;

public static class AIUtils
{
    public static double GetTimeStamp(bool isMillisecond = true)
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1);
        if (isMillisecond) { return ts.TotalMilliseconds; } else { return ts.TotalSeconds; }
    }
}