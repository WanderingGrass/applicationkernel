using System;

namespace Todd.Auth.Dates;

internal static class Extensions
{
    public static long ToTimestamp(this DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();
}