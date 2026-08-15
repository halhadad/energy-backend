namespace energy_backend.Application.Common;

public static class AnalyticsMath
{
    public static double Percent(double part, double whole) => whole > 0 ? part / whole * 100 : 0;

    public static double ProjectMonth(double monthToDateKwh, double elapsedDays, int daysInMonth)
        => elapsedDays > 0 ? monthToDateKwh * daysInMonth / elapsedDays : monthToDateKwh;

    public static double ChangePercent(double current, double previous)
        => previous > 0 ? (current - previous) / previous * 100 : 0;
}
