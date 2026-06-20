namespace energy_backend.Core.Common;

public static class TimeSlots
{
    // round a utc time down to the previous interval boundary (e.g. 14:07:08 with 5s gives 14:07:05)
    public static DateTime FloorToIntervalUtc(DateTime utcTime, int intervalSeconds)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = utcTime.ToUniversalTime();

        var intervalTicks = TimeSpan.FromSeconds(intervalSeconds).Ticks;
        return new DateTime(
            utcTime.Ticks - (utcTime.Ticks % intervalTicks),
            DateTimeKind.Utc);
    }

    public static DateTime FloorTo5sUtc(DateTime utcTime)
        => FloorToIntervalUtc(utcTime, 5);
}