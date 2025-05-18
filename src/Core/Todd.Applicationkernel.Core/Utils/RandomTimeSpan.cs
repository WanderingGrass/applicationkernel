// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Todd.Applicationkernel.Core
{
    public static class RandomTimeSpan
    {
        public static TimeSpan Next(TimeSpan timeSpan)
        {
            if (timeSpan.Ticks <= 0) throw new ArgumentOutOfRangeException(nameof(timeSpan), "TimeSpan must be positive.");
            return TimeSpan.FromTicks(Random.Shared.NextInt64(timeSpan.Ticks));
        }
        public static TimeSpan Next(TimeSpan min, TimeSpan max)
        {
            if (min.Ticks < 0 || max.Ticks < 0) throw new ArgumentOutOfRangeException("TimeSpan must be positive.");
            if (min >= max) throw new ArgumentOutOfRangeException("Min must be less than max.");
            return min + Next(min - max);
        }
    }
}
