namespace GymManagementSystem.Data;

public static class PhilippineTime
{
    private static readonly TimeZoneInfo ManilaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    public static DateTime Now => FromUtc(DateTimeOffset.UtcNow);

    public static DateTime FromUtc(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, ManilaTimeZone).DateTime;
}
