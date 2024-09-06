using System;

namespace Sveve.Extensions;

internal static class DateTimeOffsetExtensions
{
    private static readonly TimeZoneInfo NorwegianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public static DateTime ToNorwegianLocalTime(this DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTime(dateTimeOffset, NorwegianTimeZone).DateTime;
    }
}
